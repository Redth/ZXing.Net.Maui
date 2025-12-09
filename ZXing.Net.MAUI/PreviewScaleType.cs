namespace ZXing.Net.Maui;

/// <summary>
/// Defines how the camera preview should be scaled and aligned within its container.
/// "Fill" scales the preview to fill the container, possibly cropping the image.
/// "Fit" scales the preview to fit entirely within the container, possibly leaving empty space.
/// "Center", "Start", and "End" specify the alignment of the preview within the container.
/// </summary>
public enum PreviewScaleType
{
    /// <summary>
    /// Scale the preview uniformly to fill the container, cropping as needed, and center it.
    /// </summary>
    FillCenter,
    /// <summary>
    /// Scale the preview uniformly to fill the container, cropping as needed, and align it to the start (top or left).
    /// </summary>
    FillStart,
    /// <summary>
    /// Scale the preview uniformly to fill the container, cropping as needed, and align it to the end (bottom or right).
    /// </summary>
    FillEnd,
    /// <summary>
    /// Scale the preview uniformly to fit entirely within the container, centering it. No cropping, may leave empty space.
    /// </summary>
    FitCenter,
    /// <summary>
    /// Scale the preview uniformly to fit entirely within the container, aligning it to the start (top or left). No cropping, may leave empty space.
    /// </summary>
    FitStart,
    /// <summary>
    /// Scale the preview uniformly to fit entirely within the container, aligning it to the end (bottom or right). No cropping, may leave empty space.
    /// </summary>
    FitEnd,
}
