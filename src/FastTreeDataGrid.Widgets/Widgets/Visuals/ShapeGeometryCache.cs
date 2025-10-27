using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;

namespace FastTreeDataGrid.Control.Widgets;

internal static class ShapeGeometryCache
{
    private static readonly GeometryCache<LineGeometryKey, LineGeometry> LineCache = new(
        key => new LineGeometry(key.StartPoint, key.EndPoint));

    private static readonly GeometryCache<RectangleGeometryKey, RectangleGeometry> RectangleCache = new(
        key =>
        {
            var rect = new Rect(new Size(key.Width, key.Height)).Deflate(key.StrokeThickness / 2);
            return new RectangleGeometry(rect, key.RadiusX, key.RadiusY);
        });

    private static readonly GeometryCache<EllipseGeometryKey, EllipseGeometry> EllipseCache = new(
        key =>
        {
            var rect = new Rect(new Size(key.Width, key.Height)).Deflate(key.StrokeThickness / 2);
            return new EllipseGeometry(rect);
        });

    private static readonly GeometryCache<PointsGeometryKey, PolylineGeometry> PolylineCache = new(
        key =>
        {
            var points = key.GetPoints();
            var geometry = new PolylineGeometry
            {
                Points = new List<Point>(points.ToArray()),
                IsFilled = key.IsClosed
            };
            return geometry;
        });

    private static readonly GeometryCache<ArcGeometryKey, Geometry> ArcCache = new(CreateArcGeometry);

    private static readonly GeometryCache<SectorGeometryKey, Geometry> SectorCache = new(CreateSectorGeometry);

    private static readonly Dictionary<string, WeakReference<Geometry>> PathCache = new(StringComparer.Ordinal);
    private static readonly object PathSync = new();

    private static readonly StreamGeometry EmptyGeometry = new();

    public static Geometry GetLineGeometry(Point start, Point end)
    {
        var key = new LineGeometryKey(start, end);
        return LineCache.GetOrCreate(key);
    }

    public static Geometry? GetRectangleGeometry(double width, double height, double strokeThickness, double radiusX, double radiusY)
    {
        if (width <= 0 || height <= 0)
        {
            return null;
        }

        var key = new RectangleGeometryKey(width, height, radiusX, radiusY, strokeThickness);
        return RectangleCache.GetOrCreate(key);
    }

    public static Geometry? GetEllipseGeometry(double width, double height, double strokeThickness)
    {
        if (width <= 0 || height <= 0)
        {
            return null;
        }

        var key = new EllipseGeometryKey(width, height, strokeThickness);
        return EllipseCache.GetOrCreate(key);
    }

    public static Geometry? GetPolygonGeometry(Point[] points)
    {
        if (points.Length == 0)
        {
            return null;
        }

        var key = new PointsGeometryKey(points, isClosed: true);
        return PolylineCache.GetOrCreate(key);
    }

    public static Geometry? GetPolylineGeometry(Point[] points)
    {
        if (points.Length == 0)
        {
            return null;
        }

        var key = new PointsGeometryKey(points, isClosed: false);
        return PolylineCache.GetOrCreate(key);
    }

    public static Geometry? GetArcGeometry(double width, double height, double strokeThickness, double startAngle, double sweepAngle)
    {
        if (width <= 0 || height <= 0)
        {
            return null;
        }

        if (Math.Abs(sweepAngle) <= double.Epsilon)
        {
            return EmptyGeometry;
        }

        var key = new ArcGeometryKey(width, height, strokeThickness, startAngle, sweepAngle);
        return ArcCache.GetOrCreate(key);
    }

    public static Geometry? GetSectorGeometry(double width, double height, double strokeThickness, double startAngle, double sweepAngle)
    {
        if (width <= 0 || height <= 0)
        {
            return null;
        }

        if (Math.Abs(sweepAngle) <= double.Epsilon)
        {
            return EmptyGeometry;
        }

        var key = new SectorGeometryKey(width, height, strokeThickness, startAngle, sweepAngle);
        return SectorCache.GetOrCreate(key);
    }

    public static Geometry? GetPathGeometry(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            return null;
        }

        lock (PathSync)
        {
            if (PathCache.TryGetValue(data, out var reference) && reference.TryGetTarget(out var cached))
            {
                return cached;
            }

            try
            {
                var parsed = StreamGeometry.Parse(data);
                PathCache[data] = new WeakReference<Geometry>(parsed);
                return parsed;
            }
            catch
            {
                return null;
            }
        }
    }

    private static Geometry CreateArcGeometry(ArcGeometryKey key)
    {
        if (Math.Abs(key.SweepAngle) >= 360.0)
        {
            return EllipseCache.GetOrCreate(new EllipseGeometryKey(key.Width, key.Height, key.StrokeThickness));
        }

        var rect = new Rect(new Size(key.Width, key.Height));
        var deflated = rect.Deflate(key.StrokeThickness / 2);

        var angle1 = DegreesToRadians(key.StartAngle);
        var angle2 = angle1 + DegreesToRadians(key.SweepAngle);

        var start = Math.Min(angle1, angle2);
        var end = Math.Max(angle1, angle2);

        var normStart = NormalizeRadians(start);
        var normEnd = NormalizeRadians(end);

        if (normStart == normEnd && start != end)
        {
            return EllipseCache.GetOrCreate(new EllipseGeometryKey(key.Width, key.Height, key.StrokeThickness));
        }

        var center = rect.Center;
        var radiusX = deflated.Width / 2;
        var radiusY = deflated.Height / 2;
        var angleGap = NormalizeRadians(end - start);

        var startPoint = GetEllipsePoint(center, radiusX, radiusY, start);
        var endPoint = GetEllipsePoint(center, radiusX, radiusY, end);

        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(startPoint, false);
            context.ArcTo(
                endPoint,
                new Size(radiusX, radiusY),
                angleGap,
                angleGap >= Math.PI,
                SweepDirection.Clockwise);
            context.EndFigure(false);
        }

        return geometry;
    }

    private static Geometry CreateSectorGeometry(SectorGeometryKey key)
    {
        if (Math.Abs(key.SweepAngle) >= 360.0)
        {
            return EllipseCache.GetOrCreate(new EllipseGeometryKey(key.Width, key.Height, key.StrokeThickness));
        }

        var rect = new Rect(new Size(key.Width, key.Height));
        var deflated = rect.Deflate(key.StrokeThickness / 2);

        var (startAngle, endAngle) = GetMinMaxFromDelta(
            DegreesToRadians(key.StartAngle),
            DegreesToRadians(key.SweepAngle));

        var centre = new Point(rect.Width * 0.5d, rect.Height * 0.5d);
        var radiusX = deflated.Width * 0.5d;
        var radiusY = deflated.Height * 0.5d;
        var startPoint = GetEllipsePoint(centre, radiusX, radiusY, startAngle);
        var endPoint = GetEllipsePoint(centre, radiusX, radiusY, endAngle);
        var size = new Size(radiusX, radiusY);

        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(startPoint, isFilled: true);
            context.ArcTo(
                endPoint,
                size,
                rotationAngle: 0.0d,
                isLargeArc: Math.Abs(key.SweepAngle) > 180.0d,
                SweepDirection.Clockwise);
            context.LineTo(centre);
            context.EndFigure(true);
        }

        return geometry;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180d;

    private static double NormalizeRadians(double radians)
    {
        var twoPi = Math.PI * 2;
        return ((radians % twoPi) + twoPi) % twoPi;
    }

    private static (double min, double max) GetMinMaxFromDelta(double start, double delta)
    {
        if (delta >= 0)
        {
            return (start, start + delta);
        }

        return (start + delta, start);
    }

    private static Point GetEllipsePoint(Point centre, double radiusX, double radiusY, double angle)
    {
        var x = (radiusX * Math.Cos(angle)) + centre.X;
        var y = (radiusY * Math.Sin(angle)) + centre.Y;
        return new Point(x, y);
    }

    private sealed class GeometryCache<TKey, TGeometry>
        where TKey : notnull
        where TGeometry : Geometry
    {
        private readonly Dictionary<TKey, WeakReference<TGeometry>> _entries = new();
        private readonly Func<TKey, TGeometry> _factory;
        private readonly object _sync = new();

        public GeometryCache(Func<TKey, TGeometry> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public TGeometry GetOrCreate(TKey key)
        {
            lock (_sync)
            {
                if (_entries.TryGetValue(key, out var reference) && reference.TryGetTarget(out var cached))
                {
                    return cached;
                }

                var created = _factory(key);
                _entries[key] = new WeakReference<TGeometry>(created);
                return created;
            }
        }
    }

    private readonly struct LineGeometryKey : IEquatable<LineGeometryKey>
    {
        public LineGeometryKey(Point startPoint, Point endPoint)
        {
            StartPoint = startPoint;
            EndPoint = endPoint;
        }

        public Point StartPoint { get; }

        public Point EndPoint { get; }

        public bool Equals(LineGeometryKey other)
        {
            return StartPoint == other.StartPoint && EndPoint == other.EndPoint;
        }

        public override bool Equals(object? obj)
        {
            return obj is LineGeometryKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(StartPoint, EndPoint);
        }
    }

    private readonly struct RectangleGeometryKey : IEquatable<RectangleGeometryKey>
    {
        public RectangleGeometryKey(double width, double height, double radiusX, double radiusY, double strokeThickness)
        {
            Width = width;
            Height = height;
            RadiusX = radiusX;
            RadiusY = radiusY;
            StrokeThickness = strokeThickness;
            _hashCode = ComputeHashCode(width, height, radiusX, radiusY, strokeThickness);
        }

        public double Width { get; }
        public double Height { get; }
        public double RadiusX { get; }
        public double RadiusY { get; }
        public double StrokeThickness { get; }

        private readonly int _hashCode;

        public bool Equals(RectangleGeometryKey other)
        {
            return Width.Equals(other.Width)
                   && Height.Equals(other.Height)
                   && RadiusX.Equals(other.RadiusX)
                   && RadiusY.Equals(other.RadiusY)
                   && StrokeThickness.Equals(other.StrokeThickness);
        }

        public override bool Equals(object? obj)
        {
            return obj is RectangleGeometryKey other && Equals(other);
        }

        public override int GetHashCode() => _hashCode;

        private static int ComputeHashCode(double width, double height, double radiusX, double radiusY, double strokeThickness)
        {
            var hash = new HashCode();
            hash.Add(width);
            hash.Add(height);
            hash.Add(radiusX);
            hash.Add(radiusY);
            hash.Add(strokeThickness);
            return hash.ToHashCode();
        }
    }

    private readonly struct EllipseGeometryKey : IEquatable<EllipseGeometryKey>
    {
        public EllipseGeometryKey(double width, double height, double strokeThickness)
        {
            Width = width;
            Height = height;
            StrokeThickness = strokeThickness;
            _hashCode = ComputeHashCode(width, height, strokeThickness);
        }

        public double Width { get; }
        public double Height { get; }
        public double StrokeThickness { get; }

        private readonly int _hashCode;

        public bool Equals(EllipseGeometryKey other)
        {
            return Width.Equals(other.Width)
                   && Height.Equals(other.Height)
                   && StrokeThickness.Equals(other.StrokeThickness);
        }

        public override bool Equals(object? obj)
        {
            return obj is EllipseGeometryKey other && Equals(other);
        }

        public override int GetHashCode() => _hashCode;

        private static int ComputeHashCode(double width, double height, double strokeThickness)
        {
            var hash = new HashCode();
            hash.Add(width);
            hash.Add(height);
            hash.Add(strokeThickness);
            return hash.ToHashCode();
        }
    }

    private readonly struct PointsGeometryKey : IEquatable<PointsGeometryKey>
    {
        private readonly Point[] _points;
        private readonly int _hashCode;

        public PointsGeometryKey(Point[] points, bool isClosed)
        {
            IsClosed = isClosed;
            if (points.Length == 0)
            {
                _points = Array.Empty<Point>();
                _hashCode = isClosed ? 1 : 0;
                return;
            }

            _points = new Point[points.Length];
            Array.Copy(points, _points, points.Length);

            var hash = new HashCode();
            hash.Add(isClosed);
            foreach (var point in _points)
            {
                hash.Add(point.X);
                hash.Add(point.Y);
            }
            _hashCode = hash.ToHashCode();
        }

        public bool IsClosed { get; }

        public ReadOnlySpan<Point> GetPoints() => _points;

        public bool Equals(PointsGeometryKey other)
        {
            if (IsClosed != other.IsClosed || _points.Length != other._points.Length)
            {
                return false;
            }

            for (var i = 0; i < _points.Length; i++)
            {
                if (_points[i] != other._points[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object? obj)
        {
            return obj is PointsGeometryKey other && Equals(other);
        }

        public override int GetHashCode() => _hashCode;
    }

    private readonly struct ArcGeometryKey : IEquatable<ArcGeometryKey>
    {
        private readonly int _hashCode;

        public ArcGeometryKey(double width, double height, double strokeThickness, double startAngle, double sweepAngle)
        {
            Width = width;
            Height = height;
            StrokeThickness = strokeThickness;
            StartAngle = startAngle;
            SweepAngle = sweepAngle;

            var hash = new HashCode();
            hash.Add(width);
            hash.Add(height);
            hash.Add(strokeThickness);
            hash.Add(startAngle);
            hash.Add(sweepAngle);
            _hashCode = hash.ToHashCode();
        }

        public double Width { get; }
        public double Height { get; }
        public double StrokeThickness { get; }
        public double StartAngle { get; }
        public double SweepAngle { get; }

        public bool Equals(ArcGeometryKey other)
        {
            return Width.Equals(other.Width)
                   && Height.Equals(other.Height)
                   && StrokeThickness.Equals(other.StrokeThickness)
                   && StartAngle.Equals(other.StartAngle)
                   && SweepAngle.Equals(other.SweepAngle);
        }

        public override bool Equals(object? obj)
        {
            return obj is ArcGeometryKey other && Equals(other);
        }

        public override int GetHashCode() => _hashCode;
    }

    private readonly struct SectorGeometryKey : IEquatable<SectorGeometryKey>
    {
        private readonly int _hashCode;

        public SectorGeometryKey(double width, double height, double strokeThickness, double startAngle, double sweepAngle)
        {
            Width = width;
            Height = height;
            StrokeThickness = strokeThickness;
            StartAngle = startAngle;
            SweepAngle = sweepAngle;

            var hash = new HashCode();
            hash.Add(width);
            hash.Add(height);
            hash.Add(strokeThickness);
            hash.Add(startAngle);
            hash.Add(sweepAngle);
            _hashCode = hash.ToHashCode();
        }

        public double Width { get; }
        public double Height { get; }
        public double StrokeThickness { get; }
        public double StartAngle { get; }
        public double SweepAngle { get; }

        public bool Equals(SectorGeometryKey other)
        {
            return Width.Equals(other.Width)
                   && Height.Equals(other.Height)
                   && StrokeThickness.Equals(other.StrokeThickness)
                   && StartAngle.Equals(other.StartAngle)
                   && SweepAngle.Equals(other.SweepAngle);
        }

        public override bool Equals(object? obj)
        {
            return obj is SectorGeometryKey other && Equals(other);
        }

        public override int GetHashCode() => _hashCode;
    }
}
