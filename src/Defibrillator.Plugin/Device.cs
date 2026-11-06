using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Defibrillator.Core;
using Photon.Pun;
using UnityEngine;

namespace Defibrillator.Mod;

public sealed class Device : MonoBehaviour
{
    internal static readonly HashSet<Device> Active = new HashSet<Device>();
    internal ItemAttributes Attributes = null!;
    internal ItemBattery Battery = null!;
    internal PhotonView View = null!;
    private PhysGrabObject physical = null!;
    private RevivalService? registeredWith;
    private string registeredId = "";
    private bool started;
    private float nextInput;
    private bool holding;
    private int lastOperator;
    private float progress;
    private string message = "Aim at a dead player's head and hold Interact";
    private float lastFeedback;
    private AudioSource audioSource = null!;
    private Renderer[] indicators = System.Array.Empty<Renderer>();
    private readonly MaterialPropertyBlock indicatorColor = new MaterialPropertyBlock();

    internal string Id => SemiFunc.IsMultiplayer() ? "net:" + View.ViewID : "local:" + GetInstanceID();
    internal bool Ready => started && (!SemiFunc.IsMultiplayer() || View.ViewID > 0);
    internal int NetworkId => View.ViewID;

    private void OnEnable() => Active.Add(this);
    private IEnumerator Start()
    {
        Attributes = GetComponent<ItemAttributes>();
        Battery = GetComponent<ItemBattery>();
        physical = GetComponent<PhysGrabObject>();
        View = GetComponent<PhotonView>();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = 1;
        audioSource.maxDistance = 12;
        audioSource.volume = 0.45f;
        audioSource.playOnAwake = false;
        indicators = GetComponentsInChildren<Renderer>(true).Where(renderer => renderer.name.StartsWith("Cell ")).ToArray();
        // Native components must finish Start before interacting with ItemBattery.
        yield return null;
        GetComponent<Rigidbody>().ResetInertiaTensor();
        started = true;
    }

    internal bool IsHeldBy(PlayerAvatar player) => physical && physical.playerGrabbing.Any(grabber =>
        grabber && grabber.playerAvatar == player);

    internal void SyncHostBattery()
    {
        var service = Plugin.Service;
        if (!Ready || service == null) return;
        int charge = Mathf.FloorToInt(Mathf.Clamp(Battery.batteryLife, 0, 100));
        if (!ReferenceEquals(registeredWith, service) || registeredId != Id)
        {
            registeredWith?.RemoveItem(registeredId);
            service.RegisterItem(Id, charge);
            registeredWith = service;
            registeredId = Id;
        }
        else if (service.GetCharge(Id) != charge) service.UpdateChargeFromCharger(Id, charge);
    }

    private void Update()
    {
        if (!Plugin.Instance || !Plugin.Instance.Operational || !Ready || !PlayerAvatar.instance || !RunManager.instance) return;
        for (int index = 0; index < indicators.Length; index++)
        {
            Color color = Battery.batteryLife >= (index + 1) * 25 ? new Color(0.15f, 0.95f, 0.30f) : new Color(0.04f, 0.06f, 0.04f);
            indicatorColor.SetColor("_Color", color);
            indicatorColor.SetColor("_BaseColor", color);
            indicators[index].SetPropertyBlock(indicatorColor);
        }
        var player = PlayerAvatar.instance;
        bool heldLocally = IsHeldBy(player) && GameAccess.IsLocal(player);
        if (!heldLocally || !SemiFunc.RunIsLevel() || !GameAccess.InputAllowed() ||
            !GameAccess.IsAlive(player) || !SemiFunc.InputHold(InputKey.Interact))
        {
            if (holding) Plugin.Instance.Network.Send(this, lastOperator, 0, release: true);
            holding = false;
            progress = 0;
            return;
        }
        int actor = GameAccess.PlayerId(player);
        int target = FindAimedTarget(player);
        if (target == 0)
        {
            if (holding) Plugin.Instance.Network.Send(this, actor, 0, release: true);
            holding = false;
            progress = 0;
            message = "Aim at a dead player's head within range";
            return;
        }
        if (Time.unscaledTime < nextInput) return;
        nextInput = Time.unscaledTime + 0.15f;
        holding = true;
        lastOperator = actor;
        Plugin.Instance.Network.Send(this, actor, target, release: false);
    }

    private int FindAimedTarget(PlayerAvatar player)
    {
        if (!GameDirector.instance) return 0;
        int result = 0;
        double distance = double.PositiveInfinity;
        foreach (var target in GameDirector.instance.PlayerList)
        {
            if (!target || target == player) continue;
            var observation = Plugin.World.Observe(GameAccess.PlayerId(player), Id, GameAccess.PlayerId(target));
            // The host decides eligibility and range; this only chooses the aimed target.
            if (observation.TargetDead && observation.HasLineOfSight && observation.DistanceMeters < distance)
            { distance = observation.DistanceMeters; result = GameAccess.PlayerId(target); }
        }
        return result;
    }

    internal void Feedback(int operatorId, Outcome outcome, float value, int charge)
    {
        if (outcome == Outcome.Revived) audioSource.PlayOneShot(OriginalAssets.Shock);
        if (!PlayerAvatar.instance || GameAccess.PlayerId(PlayerAvatar.instance) != operatorId) return;
        progress = Mathf.Clamp01(value);
        lastFeedback = Time.unscaledTime;
        message = outcome switch
        {
            Outcome.Charging => "Charging — keep holding Interact",
            Outcome.Revived => "Revived • battery " + charge + "%",
            Outcome.EmptyBattery => "Recharge the defibrillator",
            Outcome.Busy => "Another revival is in progress",
            Outcome.Interrupted => "Revival interrupted",
            Outcome.InvalidTarget => "Target unavailable or out of range",
            Outcome.RevivalFailed => "Revival failed — no battery consumed",
            _ => "Aim at a dead player's head and hold Interact"
        };
        if (outcome == Outcome.Charging && value == 0) audioSource.PlayOneShot(OriginalAssets.StartCharge);
    }

    private void OnGUI()
    {
        if (!Ready || !PlayerAvatar.instance || !IsHeldBy(PlayerAvatar.instance) || !SemiFunc.RunIsLevel()) return;
        if (holding && Time.unscaledTime - lastFeedback > 1.5f) { progress = 0; message = "Waiting for host"; }
        var rect = new Rect(Screen.width / 2f - 190, Screen.height / 2f + 70, 380, 65);
        GUI.Box(rect, "DEFIBRILLATOR • " + Mathf.FloorToInt(Battery.batteryLife) + "%\n" + message);
        GUI.Box(new Rect(rect.x + 10, rect.y + 48, 360 * progress, 8), "");
    }

    private void OnDisable()
    {
        Active.Remove(this);
        if (holding && Plugin.Instance && Plugin.Instance.Network != null && GameManager.instance && Ready)
            Plugin.Instance.Network.Send(this, lastOperator, 0, release: true);
        registeredWith?.RemoveItem(registeredId);
        registeredWith = null;
        holding = false;
    }

    internal void ResetInput()
    {
        holding = false;
        progress = 0;
        nextInput = 0;
        message = "Aim at a dead player's head and hold Interact";
    }
}
