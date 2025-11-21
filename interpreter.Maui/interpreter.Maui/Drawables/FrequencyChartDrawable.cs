namespace interpreter.Maui.Drawables;

/// <summary>
/// Custom drawable for frequency chart visualization
/// Separated from MainPage following Single Responsibility Principle
/// </summary>
public class FrequencyChartDrawable : IDrawable
{
    private readonly float[] _orangeLineData = { 0.3f, 0.5f, 0.7f, 0.6f, 0.8f, 0.7f, 0.5f, 0.6f, 0.8f, 0.9f, 0.7f, 0.6f, 0.8f, 0.7f, 0.5f, 0.6f, 0.7f, 0.5f, 0.4f, 0.3f };
    private readonly float[] _greenLineData = { 0.4f, 0.3f, 0.5f, 0.7f, 0.6f, 0.8f, 0.9f, 0.7f, 0.6f, 0.5f, 0.6f, 0.8f, 0.7f, 0.6f, 0.7f, 0.8f, 0.6f, 0.5f, 0.3f, 0.2f };

    private const float Padding = 10f;
    private const float StrokeWidth = 3f;
    private const float PointRadius = 3f;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (dirtyRect.Width == 0 || dirtyRect.Height == 0)
            return;

        DrawLine(canvas, _orangeLineData, dirtyRect, Color.FromArgb("#72c9e4"));
        DrawLine(canvas, _greenLineData, dirtyRect, Color.FromArgb("#7cc8a5"));
    }

    private void DrawLine(ICanvas canvas, float[] data, RectF dirtyRect, Color color)
    {
        if (data.Length < 2)
            return;

        var dimensions = CalculateDimensions(dirtyRect);
        var path = CreateLinePath(data, dimensions);

        DrawPath(canvas, path, color);
        DrawPoints(canvas, data, dimensions, color);
    }

    private ChartDimensions CalculateDimensions(RectF dirtyRect)
    {
        return new ChartDimensions
        {
            AvailableWidth = dirtyRect.Width - (2 * Padding),
            AvailableHeight = dirtyRect.Height - (2 * Padding),
            Height = dirtyRect.Height
        };
    }

    private PathF CreateLinePath(float[] data, ChartDimensions dimensions)
    {
        var path = new PathF();
        float xStep = dimensions.AvailableWidth / (data.Length - 1);

        float x = Padding;
        float y = dimensions.Height - Padding - (data[0] * dimensions.AvailableHeight);
        path.MoveTo(x, y);

        for (int i = 1; i < data.Length; i++)
        {
            x = Padding + (i * xStep);
            y = dimensions.Height - Padding - (data[i] * dimensions.AvailableHeight);
            path.LineTo(x, y);
        }

        return path;
    }

    private void DrawPath(ICanvas canvas, PathF path, Color color)
    {
        canvas.StrokeColor = color;
        canvas.StrokeSize = StrokeWidth;
        canvas.DrawPath(path);
    }

    private void DrawPoints(ICanvas canvas, float[] data, ChartDimensions dimensions, Color color)
    {
        float xStep = dimensions.AvailableWidth / (data.Length - 1);
        canvas.FillColor = color;

        for (int i = 0; i < data.Length; i++)
        {
            float x = Padding + (i * xStep);
            float y = dimensions.Height - Padding - (data[i] * dimensions.AvailableHeight);
            canvas.FillCircle(x, y, PointRadius);
        }
    }

    private struct ChartDimensions
    {
        public float AvailableWidth { get; set; }
        public float AvailableHeight { get; set; }
        public float Height { get; set; }
    }
}

