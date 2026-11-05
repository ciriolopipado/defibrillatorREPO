using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

// Metadata-only checks run without loading Unity or executing any game code.
string directory = args.Length == 1 ? args[0] : "local-libs";
using var stream = File.OpenRead(Path.Combine(directory, "Assembly-CSharp.dll"));
using var pe = new PEReader(stream);
var reader = pe.GetMetadataReader();
int checks = 0;

var methods = new (string Type, string Method, int Arguments)[]
{
    ("StatsManager", "LoadItemsFromFolder", 0),
    ("ItemAttributes", "GetValue", 0),
    ("ItemAttributes", "GetValueRPC", 2),
    ("PunManager", "ShopPopulateItemVolumes", 1),
    ("PunManager", "SpawnShopItem", 5),
    ("PlayerHealth", "Hurt", 4),
    ("PlayerHealth", "UpdateHealthRPC", 5),
    ("PlayerAvatar", "PlayerDeathDone", 0),
    ("PlayerAvatar", "Revive", 1),
    ("PlayerHealth", "HealOther", 2),
    ("ItemBattery", "SetBatteryLife", 1),
    ("ItemEquippable", "GetOwnerPlayerAvatar", 0),
    ("PlayerVoiceChat", "Update", 0),
    ("PlayerVoiceChat", "OverridePitch", 6),
    ("CameraShake", "Shake", 3)
};
foreach (var required in methods)
{
    var type = FindType(required.Type);
    int count = type.GetMethods().Select(reader.GetMethodDefinition).Count(method =>
        reader.GetString(method.Name) == required.Method && Arity(method) == required.Arguments);
    Require(count == 1, $"{required.Type}.{required.Method}/{required.Arguments} resolves uniquely");
}

var fields = new (string Type, string Field)[]
{
    ("PlayerAvatar", "isDisabled"), ("PlayerAvatar", "deadSet"),
    ("PlayerAvatar", "playerDeathHead"), ("PlayerAvatar", "clientCameraPosition"),
    ("PlayerAvatar", "clientCameraRotation"), ("PlayerHealth", "health"),
    ("ItemAttributes", "shopItem"), ("ItemAttributes", "value"),
    ("PlayerController", "InputDisableTimer"), ("PlayerVoiceChat", "playerAvatar")
};
foreach (var required in fields)
{
    var type = FindType(required.Type);
    Require(type.GetFields().Select(reader.GetFieldDefinition).Count(field =>
        reader.GetString(field.Name) == required.Field) == 1, $"{required.Type}.{required.Field} exists");
}

// A ref parameter is essential for the reflective native shop-spawn call.
var spawn = FindType("PunManager").GetMethods().Select(reader.GetMethodDefinition)
    .Single(method => reader.GetString(method.Name) == "SpawnShopItem");
var spawnSignature = reader.GetBlobReader(spawn.Signature);
spawnSignature.ReadSignatureHeader();
spawnSignature.ReadCompressedInteger();
SkipType(ref spawnSignature); // Return value.
SkipType(ref spawnSignature); // ItemVolume.
SkipType(ref spawnSignature); // List<Item>.
Require(spawnSignature.ReadSignatureTypeCode() == SignatureTypeCode.ByReference &&
    spawnSignature.ReadSignatureTypeCode() == SignatureTypeCode.Int32,
    "Native shop spawn third parameter is ref int");
Console.WriteLine($"Executed {checks} game API metadata checks: all passed. No in-game behavior was executed.");
return 0;

TypeDefinition FindType(string name) => reader.GetTypeDefinition(reader.TypeDefinitions.Single(handle =>
    reader.GetString(reader.GetTypeDefinition(handle).Name) == name));

int Arity(MethodDefinition method)
{
    var signature = reader.GetBlobReader(method.Signature);
    var header = signature.ReadSignatureHeader();
    if (header.IsGeneric) signature.ReadCompressedInteger();
    return signature.ReadCompressedInteger();
}

void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException("API mismatch: " + message);
    checks++;
    Console.WriteLine("PASS " + message);
}

static void SkipType(ref BlobReader signature)
{
    switch (signature.ReadSignatureTypeCode())
    {
        case SignatureTypeCode.TypeHandle:
            signature.ReadTypeHandle();
            return;
        case SignatureTypeCode.GenericTypeInstance:
            SkipType(ref signature);
            int arguments = signature.ReadCompressedInteger();
            for (int index = 0; index < arguments; index++) SkipType(ref signature);
            return;
        case SignatureTypeCode.Boolean:
        case SignatureTypeCode.Int32:
        case SignatureTypeCode.Void:
            return;
        default:
            throw new InvalidOperationException("Unexpected native shop signature type.");
    }
}
