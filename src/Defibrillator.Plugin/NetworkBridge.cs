using System;
using System.Linq;
using Defibrillator.Core;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using REPOLib.Modules;

namespace Defibrillator.Mod;

internal sealed class NetworkBridge
{
    private const string Protocol = Plugin.Id + "/1";
    private readonly NetworkedEvent channel;
    internal NetworkBridge() => channel = new NetworkedEvent(Protocol, Receive);

    internal void ResetLocalInput()
    {
        foreach (var device in Device.Active)
            if (device && device.Ready) device.ResetInput();
    }

    internal void Send(Device device, int actor, int target, bool release)
    {
        if (SemiFunc.IsMasterClientOrSingleplayer())
            Handle(device, actor, target, release);
        else
            channel.RaiseEvent(new object[] { Protocol, release ? 1 : 0, device.NetworkId, actor, target },
                NetworkingEvents.RaiseMasterClient, SendOptions.SendReliable);
    }

    private void Receive(EventData packet)
    {
        if (!Plugin.Instance || !Plugin.Instance.Operational) return;
        if (packet.CustomData is not object[] data || data.Length < 2 ||
            data[0] is not string protocol || protocol != Protocol || data[1] is not int operation) return;
        try
        {
            if (operation == 3)
            {
                if (data.Length != 10 || PhotonNetwork.MasterClient == null ||
                    packet.Sender != PhotonNetwork.MasterClient.ActorNumber ||
                    data[2] is not int target || target <= 0 || data[3] is not float pitch ||
                    data[4] is not double expires || data[5] is not double nextShock ||
                    data[6] is not double interval || data[7] is not string round || round.Length > 256 ||
                    data[8] is not string token || token.Length > 64 || data[9] is not int sequence) return;
                VoiceDebuffs.Receive(target, new VoiceEffect(pitch, expires, nextShock, interval, round, token, sequence));
                return;
            }
            if (operation == 5)
            {
                if (data.Length != 7 || PhotonNetwork.MasterClient == null ||
                    packet.Sender != PhotonNetwork.MasterClient.ActorNumber ||
                    data[2] is not int target || target <= 0 || data[3] is not string token || token.Length > 64 ||
                    data[4] is not int sequence || sequence <= 0 || data[5] is not float strength ||
                    data[6] is not float seconds || float.IsNaN(strength) || strength < 0 || strength > 10 ||
                    float.IsNaN(seconds) || seconds < 0.1f || seconds > 2) return;
                VoiceDebuffs.Shock(target, token, sequence, strength, seconds);
                return;
            }
            if (operation == 4)
            {
                if (data.Length != 2 || !SemiFunc.IsMasterClientOrSingleplayer() ||
                    PhotonNetwork.CurrentRoom?.GetPlayer(packet.Sender) == null) return;
                var options = new RaiseEventOptions { TargetActors = new[] { packet.Sender } };
                foreach (var pair in VoiceDebuffs.Effects) SendVoice(pair.Key, pair.Value, options);
                return;
            }
            if (operation == 2)
            {
                if (data.Length != 7 || PhotonNetwork.MasterClient == null ||
                    packet.Sender != PhotonNetwork.MasterClient.ActorNumber ||
                    data[2] is not int id || data[3] is not int actor || data[4] is not int outcome ||
                    data[5] is not float progress || data[6] is not int charge ||
                    !Enum.IsDefined(typeof(Outcome), outcome) || float.IsNaN(progress) ||
                    float.IsInfinity(progress) || charge < 0 || charge > 100) return;
                var device = Device.Active.FirstOrDefault(item => item && item.Ready && item.NetworkId == id);
                if (device) device!.Feedback(actor, (Outcome)outcome, progress, charge);
                return;
            }
            if ((operation != 0 && operation != 1) || data.Length != 5 ||
                !SemiFunc.IsMasterClientOrSingleplayer() || data[2] is not int itemId ||
                data[3] is not int operatorId || data[4] is not int targetId) return;
            var player = GameAccess.FindPlayer(operatorId);
            if (!player || player.photonView.OwnerActorNr != packet.Sender) return;
            var requestedDevice = Device.Active.FirstOrDefault(item => item && item.Ready && item.NetworkId == itemId);
            if (requestedDevice) Handle(requestedDevice!, operatorId, targetId, operation == 1);
        }
        catch (Exception exception)
        {
            Plugin.Service?.CancelAll();
            Plugin.Error("Defibrillator request failed; active revivals cancelled. " + exception);
        }
    }

    private void Handle(Device device, int actor, int target, bool release)
    {
        if (Plugin.Service == null || !device.Ready) return;
        device.SyncHostBattery();
        Result result = release ? Plugin.Service.Release(actor, device.Id) : Plugin.Service.Hold(actor, device.Id, target);
        if (result.Outcome == Outcome.Revived)
        {
            device.Battery.SetBatteryLife(result.Charge);
            try { VoiceDebuffs.Revived(target); }
            catch (Exception exception) { Plugin.Error("Revival succeeded, but aftereffects could not be applied. " + exception); }
            Plugin.LogInfo("Revival completed: operator=" + actor + ", target=" + target + ", battery=" + result.Charge);
        }
        device.Feedback(actor, result.Outcome, (float)result.Progress, result.Charge);
        if (SemiFunc.IsMultiplayer())
            channel.RaiseEvent(new object[] { Protocol, 2, device.NetworkId, actor, (int)result.Outcome,
                (float)result.Progress, result.Charge }, NetworkingEvents.RaiseOthers, SendOptions.SendReliable);
    }

    internal void SendVoice(int target, VoiceEffect effect, RaiseEventOptions? options = null)
    {
        if (!SemiFunc.IsMultiplayer()) return;
        channel.RaiseEvent(new object[] { Protocol, 3, target, effect.Pitch, effect.VoiceExpiresAt,
            effect.NextShockAt, effect.ShockInterval, effect.Round, effect.Token, effect.Sequence },
            options ?? NetworkingEvents.RaiseOthers, SendOptions.SendReliable);
    }

    internal void RequestVoiceSnapshot() => channel.RaiseEvent(new object[] { Protocol, 4 },
        NetworkingEvents.RaiseMasterClient, SendOptions.SendReliable);

    internal void SendShock(int target, string token, int sequence, float strength, float seconds)
    {
        if (SemiFunc.IsMultiplayer()) channel.RaiseEvent(new object[] { Protocol, 5, target, token,
            sequence, strength, seconds }, NetworkingEvents.RaiseOthers, SendOptions.SendReliable);
    }
}
