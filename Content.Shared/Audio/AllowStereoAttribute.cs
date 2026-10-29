namespace Content.Shared.Audio;

/// <summary>
/// Marks that a <see cref="SoundSpecifier"/> field is allowed to specify audio files in stereo format.
/// </summary>
/// <remarks>
/// By default, audio files must be in mono format to avoid errors when played positionally.
/// Use this attribute to bypass this requirement for sounds that are only played globally, like music or ambient tracks.
/// </remarks>
[AttributeUsage(AttributeTargets.Field)]
public sealed class AllowStereoAttribute : Attribute;
