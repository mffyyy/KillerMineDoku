using UnityEngine;
using UnityEngine.UI;

namespace KillerMineDoku.UI
{
    public sealed class CageBorderRenderer : MaskableGraphic
    {
        [System.Flags]
        public enum Edges
        {
            None = 0,
            Top = 1,
            Right = 2,
            Bottom = 4,
            Left = 8,
            All = Top | Right | Bottom | Left
        }

        public KillerMineDokuTheme theme;
        public Edges edges = Edges.All;
        public bool active;
        public float inset = 6f;
        public float dashLength = 8f;
        public float gapLength = 3f;
        public float cornerRadius = 6f;
        [Range(2, 12)] public int cornerSegments = 6;
        [HideInInspector] public bool extendTopLeft;
        [HideInInspector] public bool extendTopRight;
        [HideInInspector] public bool extendBottomLeft;
        [HideInInspector] public bool extendBottomRight;
        [HideInInspector] public bool extendLeftTop;
        [HideInInspector] public bool extendLeftBottom;
        [HideInInspector] public bool extendRightTop;
        [HideInInspector] public bool extendRightBottom;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            var rect = rectTransform.rect;
            var thickness = active
                ? (theme != null ? theme.cageActiveLine : 4f)
                : (theme != null ? theme.cageDefaultLine : 3f);
            var left = rect.xMin + inset;
            var right = rect.xMax - inset;
            var top = rect.yMax - inset;
            var bottom = rect.yMin + inset;
            var fullLeft = rect.xMin;
            var fullRight = rect.xMax;
            var fullTop = rect.yMax;
            var fullBottom = rect.yMin;
            var topLeftCorner = HasEdge(Edges.Top) && HasEdge(Edges.Left) && !extendTopLeft && !extendLeftTop;
            var topRightCorner = HasEdge(Edges.Top) && HasEdge(Edges.Right) && !extendTopRight && !extendRightTop;
            var bottomRightCorner = HasEdge(Edges.Bottom) && HasEdge(Edges.Right) && !extendBottomRight && !extendRightBottom;
            var bottomLeftCorner = HasEdge(Edges.Bottom) && HasEdge(Edges.Left) && !extendBottomLeft && !extendLeftBottom;
            var radius = Mathf.Max(0f, Mathf.Min(cornerRadius, Mathf.Min(Mathf.Abs(right - left), Mathf.Abs(top - bottom)) * 0.5f));

            if (edges.HasFlag(Edges.Top))
            {
                var startX = extendTopLeft ? fullLeft : left + (topLeftCorner ? radius : 0f);
                var endX = extendTopRight ? fullRight : right - (topRightCorner ? radius : 0f);
                AddLine(vh, new Vector2(startX, top), new Vector2(endX, top), thickness, true);
            }

            if (edges.HasFlag(Edges.Right))
            {
                var startY = extendRightTop ? fullTop : top - (topRightCorner ? radius : 0f);
                var endY = extendRightBottom ? fullBottom : bottom + (bottomRightCorner ? radius : 0f);
                AddLine(vh, new Vector2(right, startY), new Vector2(right, endY), thickness, false);
            }

            if (edges.HasFlag(Edges.Bottom))
            {
                var startX = extendBottomRight ? fullRight : right - (bottomRightCorner ? radius : 0f);
                var endX = extendBottomLeft ? fullLeft : left + (bottomLeftCorner ? radius : 0f);
                AddLine(vh, new Vector2(startX, bottom), new Vector2(endX, bottom), thickness, true);
            }

            if (edges.HasFlag(Edges.Left))
            {
                var startY = extendLeftBottom ? fullBottom : bottom + (bottomLeftCorner ? radius : 0f);
                var endY = extendLeftTop ? fullTop : top - (topLeftCorner ? radius : 0f);
                AddLine(vh, new Vector2(left, startY), new Vector2(left, endY), thickness, false);
            }

            if (radius > 0f)
            {
                if (topLeftCorner)
                {
                    AddArc(vh, new Vector2(left + radius, top - radius), radius, 90f, 180f, thickness);
                }

                if (topRightCorner)
                {
                    AddArc(vh, new Vector2(right - radius, top - radius), radius, 0f, 90f, thickness);
                }

                if (bottomRightCorner)
                {
                    AddArc(vh, new Vector2(right - radius, bottom + radius), radius, -90f, 0f, thickness);
                }

                if (bottomLeftCorner)
                {
                    AddArc(vh, new Vector2(left + radius, bottom + radius), radius, 180f, 270f, thickness);
                }
            }
        }

        private bool HasEdge(Edges edge)
        {
            return edges.HasFlag(edge);
        }

        private void AddLine(VertexHelper vh, Vector2 start, Vector2 end, float thickness, bool horizontal)
        {
            if (active)
            {
                AddSegment(vh, start, end, thickness, horizontal);
                return;
            }

            var distance = Vector2.Distance(start, end);
            var direction = (end - start).normalized;
            var cursor = 0f;

            while (cursor < distance)
            {
                var segmentLength = Mathf.Min(dashLength, distance - cursor);
                var segmentStart = start + direction * cursor;
                var segmentEnd = segmentStart + direction * segmentLength;
                AddSegment(vh, segmentStart, segmentEnd, thickness, horizontal);
                cursor += dashLength + gapLength;
            }
        }

        private void AddSegment(VertexHelper vh, Vector2 start, Vector2 end, float thickness, bool horizontal)
        {
            var half = thickness * 0.5f;
            var direction = end - start;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            direction.Normalize();
            var normal = new Vector2(-direction.y, direction.x) * half;

            var index = vh.currentVertCount;
            var vertex = UIVertex.simpleVert;
            vertex.color = color;

            var p0 = start - normal;
            var p1 = start + normal;
            var p2 = end + normal;
            var p3 = end - normal;

            vertex.position = p0;
            vh.AddVert(vertex);
            vertex.position = p1;
            vh.AddVert(vertex);
            vertex.position = p2;
            vh.AddVert(vertex);
            vertex.position = p3;
            vh.AddVert(vertex);

            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index + 2, index + 3, index);
        }

        private void AddArc(VertexHelper vh, Vector2 center, float radius, float startDegrees, float endDegrees, float thickness)
        {
            var segments = Mathf.Max(2, cornerSegments);
            var previous = PointOnArc(center, radius, startDegrees);
            for (var i = 1; i <= segments; i++)
            {
                var t = i / (float)segments;
                var next = PointOnArc(center, radius, Mathf.Lerp(startDegrees, endDegrees, t));
                AddSegment(vh, previous, next, thickness, Mathf.Abs(next.x - previous.x) >= Mathf.Abs(next.y - previous.y));
                previous = next;
            }
        }

        private static Vector2 PointOnArc(Vector2 center, float radius, float degrees)
        {
            var radians = degrees * Mathf.Deg2Rad;
            return center + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
        }
    }
}
