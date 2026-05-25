using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AgentApp.UI.Converters;

/// <summary>
/// Converts an avatar shape name to a WPF Geometry for rendering.
/// Shapes: "person" (default), "robot", "star", "cat"
/// </summary>
public class ShapeToPathDataConverter : IValueConverter
{
    private static readonly Dictionary<string, string> ShapePaths = new()
    {
        // Head circle + shoulders arc
        ["person"] = "M 8,1 A 4,4 0 1 1 16,1 A 4,4 0 1 1 8,1 Z " +
                     "M 2,22 C 2,16 6,13 12,13 C 18,13 22,16 22,22 Z",

        // Square head with antenna and eyes, rectangular body
        ["robot"]  = "M 11,0 L 13,0 L 13,3 L 11,3 Z " +
                     "M 6,3 L 18,3 L 18,13 L 6,13 Z " +
                     "M 8.5,6.5 A 1.5,1.5 0 1 1 11.5,6.5 A 1.5,1.5 0 1 1 8.5,6.5 Z " +
                     "M 12.5,6.5 A 1.5,1.5 0 1 1 15.5,6.5 A 1.5,1.5 0 1 1 12.5,6.5 Z " +
                     "M 3,15 L 21,15 L 21,22 L 3,22 Z",

        // 5-pointed star
        ["star"]   = "M 12,1 L 14.9,9.2 L 23.5,9.2 L 16.7,14.7 L 19.1,23 " +
                     "L 12,17.8 L 4.9,23 L 7.3,14.7 L 0.5,9.2 L 9.1,9.2 Z",

        // Stylised cat: triangular ears on round head, body below
        ["cat"]    = "M 3,10 L 7,2 L 10,8 " +
                     "C 10.7,7.7 11.3,7.5 12,7.5 C 12.7,7.5 13.3,7.7 14,8 " +
                     "L 17,2 L 21,10 C 22,13 20,17 12,18 C 4,17 2,13 3,10 Z " +
                     "M 5,20 L 4,24 L 20,24 L 19,20 C 15,23 9,23 5,20 Z",
    };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var shape = value as string ?? "person";
        var data = ShapePaths.TryGetValue(shape, out var d) ? d : ShapePaths["person"];
        try { return Geometry.Parse(data); }
        catch { return Geometry.Parse(ShapePaths["person"]); }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
