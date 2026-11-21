using System;
using System.Collections.Generic;
using UnityEngine;

internal static class LandscapeChunkMesh
{
    private const byte LandSurface = 64;
    private const byte WaterSurface = 128;
    private const byte LightSurface = 255;

    public static Mesh Create(ThemeId theme, float length, float roadWidth, int variant)
    {
        var geometry = new Geometry();
        var random = new System.Random(11341 + (int)theme * 991 + variant * 257);
        float edge = roadWidth * 0.5f;

        switch (theme)
        {
            case ThemeId.Forest:
                Forest(geometry, random, length, edge);
                break;
            case ThemeId.Beach:
                Beach(geometry, random, length, edge, variant);
                break;
            case ThemeId.Moon:
                Moon(geometry, random, length, edge, variant);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(theme));
        }

        return geometry.Build($"{theme} {variant + 1}");
    }

    private static void Forest(Geometry mesh, System.Random random, float length, float edge)
    {
        Ground(mesh, length, edge, 0x947552, 0x726147, 0x71874b, 0x405b43, 0x304a42, 1.55f);
        Color32[] foliage = { Tone(0x2d5547), Tone(0x3b6951), Tone(0x507654), Tone(0x698557) };
        Color32[] fern = { Tone(0x8ca55b), Tone(0x638b54), Tone(0x53734e) };

        for (int side = -1; side <= 1; side += 2)
        {
            for (int i = 0; i < 21; i++)
            {
                float x = At(random, length, i, 21);
                float distance = i % 3 == 0 ? Range(random, 3.1f, 5.1f) : Range(random, 7f, 36f);
                float z = side * (edge + distance);
                float y = GroundHeight(x, z, length, edge, 1.55f);
                float height = Range(random, 4.0f, 8.4f) * (distance > 9f ? 1.32f : 1f);
                var foot = new Vector3(x, y - 0.13f, z);
                mesh.Tube(foot, foot + Vector3.up * height * 0.77f, height * 0.044f, height * 0.016f, 6, Tone(0x544635));
                for (int layer = 0; layer < 4; layer++)
                {
                    float tier = height * (0.25f + 0.17f * layer);
                    var start = foot + new Vector3(height * 0.012f * layer, tier, 0f);
                    float radius = height * (0.235f - 0.039f * layer);
                    Color32 tint = foliage[(i + layer + (side + 1)) % foliage.Length];
                    mesh.Tube(start, start + Vector3.up * height * 0.29f, radius, 0f, 7, tint);
                }
            }

            for (int i = 0; i < 25; i++)
            {
                float x = At(random, length, i, 25);
                float z = side * (edge + Range(random, 0.65f, 5.1f));
                var root = new Vector3(x, GroundHeight(x, z, length, edge, 1.55f), z);
                float size = Range(random, 0.26f, 0.69f);
                for (int leaf = 0; leaf < 5; leaf++)
                {
                    float angle = leaf * 1.257f + i;
                    mesh.Leaf(root, angle, size, size * 0.63f, size * 0.17f, fern[(i + leaf) % fern.Length]);
                }

                if (i % 4 == 0)
                {
                    var stone = root + new Vector3(0.58f, -0.04f, side * size);
                    mesh.Rock(stone, new Vector3(1.02f, 0.62f, 0.69f) * (size + 0.46f),
                        Tone(0x6c7462), Tone(0x8b9368), i);
                }

                if (i % 5 == 2)
                {
                    var mushroom = root + new Vector3(-0.45f, 0f, side * 0.18f);
                    mesh.Tube(mushroom, mushroom + Vector3.up * 0.19f, 0.055f, 0.047f, 5, Tone(0xe0c493));
                    mesh.Tube(mushroom + Vector3.up * 0.15f, mushroom + Vector3.up * 0.31f,
                        0.15f, 0.025f, 7, Tone(i % 2 == 0 ? 0xcb8a50 : 0xb87858));
                }
            }
        }
    }

    private static void Beach(Geometry mesh, System.Random random, float length, float edge, int variant)
    {
        // Le côté droit apparaît à droite de la caméra, qui pointe vers +X.
        const int slices = 20;
        float[] stripOffsets = { -81f, -26f, -12f, -6.5f, -4.4f, -2.5f, 0f, 0.4f, 0.9f, 2.4f, 7f, 23f, 54f };
        float[] heights = { -0.31f, -0.31f, -0.31f, -0.31f, -0.34f, -0.13f, 0f, 0f, 0f, 0.06f, 0.55f, 1.54f, 2.1f };
        int[] colors = { 0x238baf, 0x41b5ba, 0x68c9c0, 0xa7dfcb, 0xccc59a, 0xe5cb96, 0xebd6a5, 0xe9d1a1,
            0xebd7a9, 0xe9d2a0, 0xd9c190, 0xcbb988, 0xb5ad87 };
        var points = new Vector3[stripOffsets.Length];
        for (int slice = 0; slice < slices; slice++)
        {
            float from = length * slice / slices;
            float to = length * (slice + 1) / slices;
            for (int band = 0; band < stripOffsets.Length; band++)
                points[band] = Shore(from, length, edge, stripOffsets[band], heights[band]);
            for (int band = 0; band < stripOffsets.Length - 1; band++)
            {
                byte surface = band < 4 ? WaterSurface : LandSurface;
                mesh.Quad(points[band], points[band + 1],
                    Shore(to, length, edge, stripOffsets[band + 1], heights[band + 1]),
                    Shore(to, length, edge, stripOffsets[band], heights[band]),
                    Tone(colors[band], surface), Tone(colors[band + 1], surface));
            }
        }

        for (int i = 0; i < 11; i++)
        {
            float x = At(random, length, i, 11);
            float z = edge + Range(random, 1.8f, 10f);
            float y = ShoreHeight(x, length, z - edge);
            Palm(mesh, new Vector3(x, y - 0.1f, z), Range(random, 3.7f, 6.4f), i + variant);
            for (int tuft = 0; tuft < 7; tuft++)
            {
                float grassZ = edge + Range(random, 0.9f, 5f);
                float grassX = x + Range(random, -0.8f, 0.8f);
                mesh.Leaf(new Vector3(grassX, ShoreHeight(grassX, length, grassZ - edge), grassZ),
                    Range(random, 0f, 6.28f), 0.28f, Range(random, 0.16f, 0.52f), 0.08f, Tone(0x9caa70));
            }

            if (i % 2 == 0)
                mesh.Rock(new Vector3(x, -0.075f, -edge - 2.3f), new Vector3(0.7f, 0.26f, 0.45f),
                    Tone(0xa9987d), Tone(0xc8b699), i);
        }

        if (variant % 2 == 0)
        {
            var basePoint = new Vector3(length * 0.63f, -0.08f, -edge - 3.15f);
            mesh.Tube(basePoint, basePoint + Vector3.up * 0.35f, 1.23f, 1.12f, 8, Tone(0x969689));
            for (int band = 0; band < 5; band++)
            {
                var bottom = basePoint + Vector3.up * (0.34f + band * 0.66f);
                mesh.Tube(bottom, bottom + Vector3.up * 0.66f, 0.66f - band * 0.065f, 0.595f - band * 0.065f,
                    8, Tone(band % 2 == 0 ? 0xede5cf : 0xb9695d));
            }
            var top = basePoint + Vector3.up * 3.6f;
            mesh.Tube(top, top + Vector3.up * 0.52f, 0.36f, 0.36f, 8, Tone(0xffe0a2, LightSurface));
            mesh.Tube(top + Vector3.up * 0.49f, top + Vector3.up * 0.95f, 0.53f, 0f, 8, Tone(0x9d554d));
        }
    }

    private static Vector3 Shore(float x, float length, float edge, float offset, float height)
    {
        float phase = x * Mathf.PI * 2f / length;
        float surf = Mathf.Sin(phase) * 0.76f + Mathf.Sin(phase * 2f) * 0.36f;
        float z = offset > 0f ? edge + offset : -edge + offset;
        return new Vector3(x, height + (offset > 0.9f ? Mathf.Sin(phase) * Mathf.Min(0.18f, height * 0.3f) : 0f),
            z + (offset < -2.4f ? surf : 0f));
    }

    private static float ShoreHeight(float x, float length, float offset)
    {
        float height = offset < 7f ? Mathf.Lerp(0.06f, 0.55f, (offset - 2.4f) / 4.6f)
            : Mathf.Lerp(0.55f, 1.54f, (offset - 7f) / 16f);
        return height + Mathf.Sin(x * Mathf.PI * 2f / length) * Mathf.Min(0.18f, height * 0.3f);
    }

    private static void Palm(Geometry mesh, Vector3 root, float height, int variant)
    {
        var bend = new Vector3(0.4f * Mathf.Sin(variant), 0f, -height * 0.12f);
        Vector3 previous = root;
        for (int segment = 1; segment <= 5; segment++)
        {
            float progress = segment / 5f;
            var next = root + Vector3.up * (height * progress) + bend * (progress * progress);
            float radius = height * (0.055f - progress * 0.028f);
            mesh.Tube(previous, next, radius + height * 0.0056f, radius, 7,
                Tone(segment % 2 == 0 ? 0xa78764 : 0x9a785a));
            previous = next;
        }
        for (int frond = 0; frond < 9; frond++)
        {
            float angle = variant * 0.42f + frond * 0.698f;
            mesh.Leaf(previous, angle, height * (frond % 2 == 0 ? 0.43f : 0.34f),
                frond % 2 == 0 ? -0.47f : 0.37f, height * 0.055f,
                Tone(frond % 3 == 0 ? 0x758f59 : frond % 3 == 1 ? 0x5b8c64 : 0x4d7863));
        }
    }

    private static void Moon(Geometry mesh, System.Random random, float length, float edge, int variant)
    {
        Ground(mesh, length, edge, 0x6d7585, 0x596475, 0x657284, 0x626d7f, 0x536077, 0.83f);
        for (int side = -1; side <= 1; side += 2)
        {
            for (int i = 0; i < 9; i++)
            {
                float x = (i + 0.5f) * length / 9f;
                mesh.Box(new Vector3(x, 0.015f, side * (edge + 0.03f)), new Vector3(0.75f, 0.028f, 0.29f),
                    Tone(0xb0dbe0, LightSurface));
            }

            for (int i = 0; i < 11; i++)
            {
                float x = At(random, length, i, 11);
                float z = side * (edge + Range(random, 1.55f, 9f));
                float size = Range(random, 0.32f, 1.26f);
                float y = GroundHeight(x, z, length, edge, 0.83f);
                mesh.Rock(new Vector3(x, y - 0.08f, z), new Vector3(1.15f, 0.51f, 0.86f) * size,
                    Tone(0x747b87), Tone(0x9aa3ad), i);
            }

            for (int i = 0; i < 5; i++)
            {
                float x = (i + 0.5f) * length / 5f;
                float z = side * (edge + Range(random, 6.8f, 21f));
                float y = GroundHeight(x, z, length, edge, 0.83f);
                Crater(mesh, new Vector3(x, y, z), Range(random, 2.7f, 4.8f), i);
                float ridgeZ = side * (edge + Range(random, 27f, 46f));
                float ridgeY = GroundHeight(x, ridgeZ, length, edge, 0.83f);
                mesh.Rock(new Vector3(x, ridgeY - 0.05f, ridgeZ), new Vector3(length / 5f, Range(random, 2f, 5.5f), 7.6f),
                    Tone(0x6c788a), Tone(0x838c9e), i);
            }
        }

        float stationX = length * 0.24f;
        float stationZ = (variant == 1 ? -1f : 1f) * (edge + 5.2f);
        float stationY = GroundHeight(stationX, stationZ, length, edge, 0.83f);
        var station = new Vector3(stationX, stationY, stationZ);
        mesh.Box(station + Vector3.up * 0.16f, new Vector3(1.65f, 0.32f, 1.08f), Tone(0x8a929d));
        mesh.Box(station + Vector3.up * 0.92f, new Vector3(0.18f, 1.54f, 0.18f), Tone(0xb6b7ba));
        mesh.Box(station + Vector3.up * 1.61f, new Vector3(0.44f, 0.11f, 4.37f), Tone(0x9da9b3));
        for (int panel = -1; panel <= 1; panel += 2)
        {
            var center = station + new Vector3(0f, 1.7f, panel * 1.46f);
            var rise = new Vector3(0.86f, 0.74f, 0f);
            for (int cell = -2; cell <= 2; cell++)
            {
                var cellZ = Vector3.forward * cell * 0.35f;
                mesh.Panel(center + cellZ, rise, Vector3.back * 0.173f, Tone(0x9eadbf));
                mesh.Panel(center + cellZ + new Vector3(-0.018f, 0.026f, 0f), rise * 0.94f, Vector3.back * 0.153f,
                    Tone(cell % 2 == 0 ? 0x456086 : 0x52729b));
            }
        }
    }

    private static void Crater(Geometry mesh, Vector3 center, float radius, int variant)
    {
        const int sides = 11;
        for (int i = 0; i < sides; i++)
        {
            float angle = (i + variant) * Mathf.PI * 2f / sides;
            float nextAngle = angle + Mathf.PI * 2f / sides;
            var a = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            var b = new Vector3(Mathf.Cos(nextAngle), 0f, Mathf.Sin(nextAngle)) * radius;
            Vector3 rim = Vector3.up * 0.46f;
            Vector3 floor = Vector3.up * 0.10f;
            mesh.Quad(center + a, center + a * 0.77f + rim, center + b * 0.77f + rim, center + b,
                Tone(0x6e7a8a), Tone(0x9ba4ad));
            mesh.Quad(center + a * 0.77f + rim, center + a * 0.42f + floor,
                center + b * 0.42f + floor, center + b * 0.77f + rim,
                Tone(0x8792a0), Tone(0x4d566b));
            mesh.Triangle(center + a * 0.42f + floor, center + floor, center + b * 0.42f + floor, Tone(0x555d70));
        }
    }

    private static void Ground(Geometry mesh, float length, float edge, int lane, int inner, int verge, int field, int distance, float rise)
    {
        const int slices = 16;
        float[] widths = { 0.42f, 0f, 0.65f, 3.1f, 12f, 26f, 68f };
        int[] colors = { lane, inner, verge, field, field, distance, distance };
        for (int slice = 0; slice < slices; slice++)
        {
            float x0 = length * slice / slices;
            float x1 = length * (slice + 1) / slices;
            mesh.Quad(new Vector3(x0, 0f, -edge + 0.42f), new Vector3(x0, 0f, edge - 0.42f),
                new Vector3(x1, 0f, edge - 0.42f), new Vector3(x1, 0f, -edge + 0.42f),
                Tone(lane, LandSurface), Tone(lane, LandSurface));
            for (int side = -1; side <= 1; side += 2)
            for (int strip = 0; strip < widths.Length - 1; strip++)
            {
                float z0 = side * (edge + (strip == 0 ? -widths[0] : widths[strip]));
                float z1 = side * (edge + widths[strip + 1]);
                var a = new Vector3(x0, GroundHeight(x0, z0, length, edge, rise), z0);
                var b = new Vector3(x0, GroundHeight(x0, z1, length, edge, rise), z1);
                var c = new Vector3(x1, GroundHeight(x1, z1, length, edge, rise), z1);
                var d = new Vector3(x1, GroundHeight(x1, z0, length, edge, rise), z0);
                if (side < 0)
                    mesh.Quad(d, c, b, a, Tone(colors[strip], LandSurface), Tone(colors[strip + 1], LandSurface));
                else
                    mesh.Quad(a, b, c, d, Tone(colors[strip], LandSurface), Tone(colors[strip + 1], LandSurface));
            }
        }
    }

    private static float GroundHeight(float x, float z, float length, float edge, float rise)
    {
        float distance = Mathf.Max(0f, Mathf.Abs(z) - edge);
        float blend = 1f - Mathf.Exp(-distance * 0.082f);
        float phase = x * Mathf.PI * 2f / length;
        return blend * (rise + Mathf.Sin(phase + distance * 0.07f) * rise * 0.48f)
            - Mathf.Min(distance * 0.081f, 0.39f);
    }

    private static float At(System.Random random, float length, int index, int count)
        => (index + Range(random, 0.28f, 0.72f)) * length / count;

    private static float Range(System.Random random, float min, float max)
        => min + (float)random.NextDouble() * (max - min);

    private static Color32 Tone(int rgb, byte surface = 0)
        => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, surface);

    private sealed class Geometry
    {
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector3> normals = new List<Vector3>();
        private readonly List<Color32> colors = new List<Color32>();
        private readonly List<int> indices = new List<int>();

        public void Triangle(Vector3 a, Vector3 b, Vector3 c, Color32 color)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            indices.Add(vertices.Count);
            indices.Add(vertices.Count + 1);
            indices.Add(vertices.Count + 2);
            Add(a, normal, color);
            Add(b, normal, color);
            Add(c, normal, color);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color32 near, Color32 far)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            int first = vertices.Count;
            Add(a, normal, near);
            Add(b, normal, far);
            Add(c, normal, far);
            Add(d, normal, near);
            indices.Add(first);
            indices.Add(first + 1);
            indices.Add(first + 2);
            indices.Add(first);
            indices.Add(first + 2);
            indices.Add(first + 3);
        }

        public void Panel(Vector3 center, Vector3 u, Vector3 v, Color32 color)
            => Quad(center - u - v, center + u - v, center + u + v, center - u + v, color, color);

        public void Box(Vector3 center, Vector3 size, Color32 color)
        {
            var x = Vector3.right * (size.x * 0.5f);
            var y = Vector3.up * (size.y * 0.5f);
            var z = Vector3.forward * (size.z * 0.5f);
            Panel(center + x, y, z, color);
            Panel(center - x, z, y, color);
            Panel(center + y, z, x, color);
            Panel(center - y, x, z, color);
            Panel(center + z, x, y, color);
            Panel(center - z, y, x, color);
        }

        public void Tube(Vector3 start, Vector3 end, float baseRadius, float tipRadius, int sides, Color32 color)
        {
            var direction = (end - start).normalized;
            var u = Vector3.Cross(direction, Vector3.forward).normalized;
            var v = Vector3.Cross(u, direction);
            for (int i = 0; i < sides; i++)
            {
                float angle = (i + 0.38f) * Mathf.PI * 2f / sides;
                float next = angle + Mathf.PI * 2f / sides;
                Vector3 a = u * Mathf.Cos(angle) + v * Mathf.Sin(angle);
                Vector3 b = u * Mathf.Cos(next) + v * Mathf.Sin(next);
                if (tipRadius > 0f)
                {
                    Quad(start + a * baseRadius, end + a * tipRadius, end + b * tipRadius, start + b * baseRadius, color, color);
                    Triangle(end, end + b * tipRadius, end + a * tipRadius, color);
                }
                else
                {
                    Triangle(start + a * baseRadius, end, start + b * baseRadius, color);
                    Triangle(start, start + a * baseRadius, start + b * baseRadius, color);
                }
            }
        }

        public void Rock(Vector3 center, Vector3 size, Color32 baseColor, Color32 topColor, int variant)
        {
            const int sides = 6;
            var tip = center + new Vector3(size.x * 0.12f, size.y, -size.z * 0.12f);
            for (int i = 0; i < sides; i++)
            {
                float angle = (i + 0.28f * variant) * Mathf.PI * 2f / sides;
                float next = angle + Mathf.PI * 2f / sides;
                var a = new Vector3(Mathf.Cos(angle) * size.x, 0f, Mathf.Sin(angle) * size.z);
                var b = new Vector3(Mathf.Cos(next) * size.x, 0f, Mathf.Sin(next) * size.z);
                var shoulder = Vector3.up * size.y * 0.46f;
                var innerA = center + a * 0.71f + shoulder;
                var innerB = center + b * 0.71f + shoulder;
                Quad(center + a, innerA, innerB, center + b, baseColor, topColor);
                Triangle(innerA, tip, innerB, topColor);
            }
        }

        public void Leaf(Vector3 root, float angle, float length, float height, float width, Color32 color)
        {
            var forward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            var side = new Vector3(forward.z, 0f, -forward.x) * width;
            var middle = root + forward * length * 0.49f + Vector3.up * (0.18f * length + height * 0.54f);
            var ridge = middle + Vector3.up * width * 0.32f;
            var tip = root + forward * length + Vector3.up * height;
            Triangle(root, middle - side, ridge, color);
            Triangle(root, ridge, middle + side, color);
            Triangle(ridge, middle - side, tip, color);
            Triangle(ridge, tip, middle + side, color);
        }

        private void Add(Vector3 vertex, Vector3 normal, Color32 color)
        {
            vertices.Add(vertex);
            normals.Add(normal);
            colors.Add(color);
        }

        public Mesh Build(string name)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);
            return mesh;
        }
    }
}
