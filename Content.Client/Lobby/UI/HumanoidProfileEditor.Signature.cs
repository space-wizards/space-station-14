using Content.Shared.Signature;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private void UpdateSignature()
    {
        if (Profile == null)
            return;

        Signature.SetSignature(Profile.SignatureData);
    }

    private void SetSignatureData(SignatureData? newSignatureData)
    {
        if (newSignatureData is null)
            return;

        Profile = Profile?.WithSignatureData(newSignatureData.Clone());
        SetDirty();
    }
}
