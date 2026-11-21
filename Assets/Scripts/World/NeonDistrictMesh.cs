using System;
using System.Collections.Generic;
using UnityEngine;

internal static class NeonDistrictMesh
{
    private static readonly Color32 Asphalt = Tone(0x222637);
    private static readonly Color32 Sidewalk = Tone(0x262538);
    private static readonly Color32 Facade = Tone(0x2d293f);
    private static readonly Color32 Glass = Tone(0x171c30);
    private static readonly Color32 Trim = Tone(0x3d405a);
    private static readonly Color32 Cyan = Tone(0x42e7ff, 225);
    private static readonly Color32 Pink = Tone(0xf94ac4, 225);
    private static readonly Color32 Blue = Tone(0x4b7aff, 175);
    private static readonly Color32 Lilac = Tone(0xb58aff, 210);
    private static readonly Color32 Amber = Tone(0xffa65e, 170);
    private static readonly string[] Signs = { "NEON", "NOVA", "DATA", "METRO" };
    // Chaque ligne du glyphe est encodée sur trois pixels.
    private static readonly Dictionary<char, int[]> Glyphs = new Dictionary<char, int[]>
    {
        ['A'] = new[] { 2, 5, 7, 5, 5 },
        ['D'] = new[] { 6, 5, 5, 5, 6 },
        ['E'] = new[] { 7, 4, 6, 4, 7 },
        ['M'] = new[] { 5, 7, 7, 5, 5 },
        ['N'] = new[] { 5, 7, 7, 7, 5 },
        ['O'] = new[] { 7, 5, 5, 5, 7 },
        ['R'] = new[] { 6, 5, 6, 5, 5 },
        ['T'] = new[] { 7, 2, 2, 2, 2 },
        ['V'] = new[] { 5, 5, 5, 5, 2 },
    };

    public static Mesh Create(float length, float roadWidth, int variant)
    {
        var geometry = new Geometry();
        var random = new System.Random(6079 + variant * 257);
        float halfRoad = roadWidth * 0.5f;
        Street(geometry, random, length, halfRoad);

        // Le décor et tous les porte-à-faux commencent après les trottoirs.
        const int buildings = 5;
        float blockLength = length / buildings;
        for (int side = -1; side <= 1; side += 2)
        {
            Color32 neon = side == -1 ? Cyan : Pink;
            for (int i = 0; i < buildings; ++i)
            {
                float width = blockLength * Range(random, 0.58f, 0.84f);
                float x = (i + 0.5f) * blockLength;
                float height = Range(random, 6.2f, 14.5f);
                Building(geometry, random, x, side, halfRoad + Range(random, 2.4f, 3.7f), width,
                    Range(random, 4.4f, 6.1f), height, neon, (i + variant + (side + 1)) % Signs.Length);
                Skyline(geometry, random, x, side * (halfRoad + Range(random, 15f, 19f)),
                    blockLength * 0.84f, Range(random, 16f, 25f), neon);
            }
        }

        return geometry.ToMesh(variant);
    }

    private static void Street(Geometry geometry, System.Random random, float length, float halfRoad)
    {
        geometry.Box(length / 2f, -0.08f, 0f, length, 0.16f, halfRoad * 2f + 52f, Asphalt);
        for (int side = -1; side <= 1; side += 2)
        {
            Color32 neon = side == -1 ? Cyan : Pink;
            geometry.Box(length / 2f, 0.048f, side * (halfRoad + 1.1f), length, 0.12f, 2.2f, Sidewalk);
            geometry.Box(length / 2f, 0.063f, side * (halfRoad + 0.08f), length, 0.15f, 0.15f, Trim);
            geometry.Box(length / 2f, 0.015f, side * (halfRoad - 0.055f), length, 0.025f, 0.05f, neon);
            for (int i = 0; i < 8; ++i)
            {
                float x = (i + 0.5f) * length / 8f;
                // Les reflets imprimés au sol restent visibles sur les appareils sans bloom.
                geometry.Box(x, 0.005f, side * (halfRoad - 0.26f), Range(random, 1.2f, 4.8f),
                    0.008f, 0.30f, Tone(side == -1 ? 0x214257 : 0x502547));
                geometry.Box(x, 0.015f, side * halfRoad * 0.35f, 1.15f, 0.025f, 0.05f, Tone(0x465069, 75));
            }

            for (int i = 0; i < 3; ++i)
            {
                float x = (i + 0.5f) * length / 3f;
                float z = side * (halfRoad + 1.05f);
                geometry.Box(x, 0.24f, z, 0.42f, 0.26f, 0.38f, Trim);
                geometry.Box(x, 1.82f, z, 0.10f, 3.1f, 0.10f, Glass);
                geometry.Box(x, 3.25f, z, 0.14f, 0.24f, 0.62f, Trim);
                geometry.Box(x, 3.15f, z, 0.11f, 0.044f, 0.55f, neon);
                geometry.Box(x, 1.1f, z - side * 0.056f, 0.048f, 0.78f, 0.014f, Lilac);
            }
        }
    }

    private static void Building(Geometry geometry, System.Random random, float x, int side, float facadeZ,
                                 float width, float depth, float height, Color32 neon, int sign)
    {
        float innerZ = facadeZ * side;
        float middleZ = innerZ + side * depth * 0.5f;
        geometry.Box(x, height / 2f, middleZ, width, height, depth, Facade);
        geometry.Box(x, 0.18f, middleZ, width + 0.32f, 0.38f, depth + 0.32f, Glass);
        geometry.Box(x, height - 0.18f, middleZ, width + 0.20f, 0.35f, depth + 0.20f, Trim);
        geometry.Box(x, height - 0.18f, innerZ - side * 0.11f, width + 0.20f, 0.055f, 0.04f, neon);
        geometry.Box(x, height + 0.34f, middleZ, width * 0.57f, 0.65f, depth * 0.65f, Glass);

        float trimZ = innerZ - side * 0.065f;
        int facing = -side;
        for (int sideOfFacade = -1; sideOfFacade <= 1; sideOfFacade += 2)
        {
            float trimX = x + sideOfFacade * (width * 0.5f - 0.14f);
            geometry.Box(trimX, height * 0.5f, trimZ, 0.10f, height - 0.6f, 0.07f, Trim);
            geometry.Box(trimX, height * 0.57f, trimZ - side * 0.05f, 0.032f, height * 0.72f, 0.02f, neon);
        }

        int rows = Mathf.FloorToInt((height - 3.2f) / 1.25f);
        for (int row = 0; row < rows; ++row)
        {
            float y = 2.4f + 1.25f * row;
            geometry.Box(x, y + 0.44f, trimZ, width - 0.38f, 0.09f, 0.09f, Glass);
            for (int col = -2; col <= 2; ++col)
            {
                float windowX = x + col * width / 5.7f;
                geometry.Panel(windowX, y, trimZ, width / 7.2f, 0.65f, facing, Glass);
                if (random.NextDouble() > 0.25)
                    geometry.Panel(windowX, y, trimZ - side * 0.028f, width / 8.4f, 0.48f, facing,
                        col % 2 == 0 ? Blue : (row % 3 == 0 ? Lilac : neon));
            }

            for (int facade = -1; facade <= 1; facade += 2)
            for (int col = 1; col <= 3; ++col)
            {
                if (random.NextDouble() > 0.58) continue;
                geometry.EndPanel(x + facade * (width * 0.5f + 0.018f), y, innerZ + side * depth * col / 4f,
                    depth / 6.4f, 0.46f, facade, col % 2 == 0 ? Lilac : Blue);
            }
        }

        geometry.Box(x, 0.71f, trimZ, width * 0.56f, 1.33f, 0.12f, Glass);
        if (height > 8.5f)
            Billboard(geometry, x + 0.15f, height * 0.60f, trimZ - side * 0.10f, facing, Signs[sign], neon);
        else
            Billboard(geometry, x, 1.56f, trimZ - side * 0.10f, facing, Signs[sign], neon);
    }

    private static void Billboard(Geometry geometry, float x, float y, float z, int towardRoad, string label, Color32 neon)
    {
        const float pixel = 0.15f;
        float width = (label.Length * 4 + 1) * pixel;
        geometry.Box(x, y, z, width + 0.14f, 1.14f, 0.18f, Trim);
        geometry.Box(x, y, z + towardRoad * 0.10f, width, 0.96f, 0.038f, Glass);
        geometry.Box(x, y - 0.47f, z + towardRoad * 0.128f, width, 0.04f, 0.025f, neon);
        for (int letter = 0; letter < label.Length; ++letter)
        {
            int[] glyph = Glyphs[label[letter]];
            for (int row = 0; row < glyph.Length; ++row)
            for (int col = 0; col < 3; ++col)
            {
                if ((glyph[row] & (1 << (2 - col))) == 0) continue;
                float dx = (letter * 4 + col - (label.Length * 4 - 2) * 0.5f) * pixel;
                geometry.Panel(x - towardRoad * dx, y + (2 - row) * pixel, z + towardRoad * 0.13f,
                    pixel * 0.91f, pixel * 0.91f, towardRoad, neon);
            }
        }
    }

    private static void Skyline(Geometry geometry, System.Random random, float x, float z, float width,
                                float height, Color32 neon)
    {
        geometry.Box(x, height * 0.5f, z, width, height, 6.2f, Tone(0x201b37));
        geometry.Box(x, height - 0.10f, z, width + 0.12f, 0.15f, 6.3f, Blue);
        geometry.Box(x, height + 0.66f, z, width * 0.62f, 1.32f, 4.1f, Glass);
        geometry.Box(x, height + 2.20f, z, 0.08f, 2.2f, 0.08f, Trim);
        geometry.Box(x, height + 3.30f, z, 0.13f, 0.20f, 0.13f, Amber);
        int facing = z > 0 ? -1 : 1;
        float face = z + facing * 3.12f;
        for (int i = 1; i <= 6; ++i)
        {
            Color32 light = random.Next(3) == 0 ? neon : Lilac;
            geometry.Panel(x, i * height / 7f, face, width * 0.70f, 0.10f, facing, light);
        }
    }

    private static float Range(System.Random random, float min, float max) =>
        Mathf.Lerp(min, max, (float)random.NextDouble());

    private static Color32 Tone(int rgb, byte glow = 0)
    {
        Color32 color = ThemePalette.Rgb(rgb);
        color.a = glow;
        return color;
    }

    private sealed class Geometry
    {
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector3> normals = new List<Vector3>();
        private readonly List<Color32> colors = new List<Color32>();
        private readonly List<int> triangles = new List<int>();

        public void Box(float x, float y, float z, float width, float height, float depth, Color32 color)
        {
            var c = new Vector3(x, y, z);
            var h = new Vector3(width, height, depth) * 0.5f;
            Vector3 a = new Vector3(h.x, 0f, 0f);
            Vector3 b = new Vector3(0f, h.y, 0f);
            Vector3 d = new Vector3(0f, 0f, h.z);
            Face(c + a, b, d, Vector3.right, color);
            Face(c - a, d, b, Vector3.left, color);
            Face(c + b, d, a, Vector3.up, color);
            Face(c - b, a, d, Vector3.down, color);
            Face(c + d, a, b, Vector3.forward, color);
            Face(c - d, b, a, Vector3.back, color);
        }

        public void Panel(float x, float y, float z, float width, float height, int facing, Color32 color)
        {
            Face(new Vector3(x, y, z), Vector3.right * (width * 0.5f * facing),
                Vector3.up * (height * 0.5f), Vector3.forward * facing, color);
        }

        public void EndPanel(float x, float y, float z, float width, float height, int facing, Color32 color)
        {
            Face(new Vector3(x, y, z), Vector3.up * (height * 0.5f),
                Vector3.forward * (width * 0.5f * facing), Vector3.right * facing, color);
        }

        private void Face(Vector3 center, Vector3 u, Vector3 v, Vector3 normal, Color32 color)
        {
            int start = vertices.Count;
            vertices.Add(center - u - v);
            vertices.Add(center + u - v);
            vertices.Add(center + u + v);
            vertices.Add(center - u + v);
            for (int i = 0; i < 4; ++i)
            {
                normals.Add(normal);
                colors.Add(color);
            }
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 3);
        }

        public Mesh ToMesh(int variant)
        {
            var mesh = new Mesh { name = $"District Néon {variant + 1}" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);
            return mesh;
        }
    }
}
