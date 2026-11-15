using UnityEngine;
using UnityEngine.UI;

/// <summary>Resolution-independent terminal illustration with role-specific equipment.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class MenuOperatorPortrait : MaskableGraphic
{
    public int role;
    VertexHelper mesh;
    Color ink;
    Color shadow;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        mesh = vh;
        ink = color;
        shadow = new Color(0.055f, 0.039f, 0.025f, color.a);

        // Fine technical grid and targeting corners behind the equipment silhouette.
        Color grid = new Color(ink.r, ink.g, ink.b, 0.09f * ink.a);
        for (int i = 1; i < 10; i++)
        {
            Box(i * 10, 6, 0.3f, 88, grid);
            Box(8, i * 10, 84, 0.3f, grid);
        }
        Color detail = Color.Lerp(shadow, ink, 0.55f);
        for (int i = 0; i < 2; i++)
        {
            float x = i == 0 ? 8 : 84;
            Box(x, 87, 8, 0.8f, detail);
            Box(i == 0 ? 8 : 91, 80, 0.8f, 8, detail);
        }

        // Layered field jacket, sleeves, collar, helmet and respirator.
        Poly(detail, 17,12, 20,41, 31,51, 42,55, 58,55, 70,50, 80,40, 84,12);
        Poly(ink, 27,12, 28,41, 39,50, 61,50, 73,41, 74,12);
        Poly(shadow, 38,52, 43,57, 57,57, 62,52, 56,42, 44,42);
        Poly(detail, 38,58, 35,66, 36,80, 42,87, 58,87, 64,80, 65,66, 61,58, 55,53, 45,53);
        Poly(ink, 33,76, 35,87, 41,94, 58,94, 65,86, 67,76);
        Box(32,75,36,3,shadow);
        Poly(shadow, 38,72, 62,72, 59,64, 41,64);
        Box(41,69,18,1.2f,ink);
        Poly(shadow, 43,60, 47,65, 54,65, 59,59, 57,54, 45,54);
        Box(48,55,5,6,detail);
        Box(40,16,3,28,shadow);
        Box(59,16,3,28,shadow);
        Box(48,12,2,30,shadow);
        Box(31,27,12,9,detail);
        Box(57,27,12,9,detail);
        Box(30,12,40,4,shadow);
        Box(46,12,9,4,detail);
        Box(22,18,2,20,shadow);
        Box(78,18,2,20,shadow);

        if (role == 0) // Mechanic: lamp, reinforced apron and tool pockets.
        {
            Box(46,81,10,10,shadow);
            Box(48,83,6,6,ink);
            Box(35,18,30,17,shadow);
            for (int i = 0; i < 3; i++) Box(39+i*8,21,3,11,detail);
            Box(66,40,5,8,shadow);
        }
        else if (role == 1) // Scout: binocular optics, radio and diagonal harness.
        {
            Box(36,65,12,9,shadow);
            Box(52,65,12,9,shadow);
            Box(39,68,6,3,ink);
            Box(55,68,6,3,ink);
            Poly(shadow, 30,42, 34,46, 69,20, 65,16);
            Box(66,34,7,13,shadow);
            Box(70,47,1,15,ink);
        }
        else // Medic: clear cross emblem and medical pouches.
        {
            Box(52,29,17,15,shadow);
            Box(59,32,3,9,ink);
            Box(56,35,9,3,ink);
            Box(34,18,11,9,shadow);
            Box(56,18,11,9,shadow);
            Box(47,82,7,10,shadow);
            Box(43,85,15,4,shadow);
        }

        // Short calibration ticks, intentionally outside the portrait's silhouette.
        for (int i = 0; i < 7; i++) Box(8,18+i*7,i % 3 == 0 ? 5 : 2,0.6f,detail);
    }

    void Box(float x, float y, float w, float h, Color tint)
    {
        Poly(tint, x,y, x+w,y, x+w,y+h, x,y+h);
    }

    void Poly(Color tint, params float[] points)
    {
        Rect r = rectTransform.rect;
        int start = mesh.currentVertCount;
        for (int i = 0; i < points.Length; i += 2)
            mesh.AddVert(new Vector3(r.xMin + points[i] * r.width / 100f,
                r.yMin + points[i+1] * r.height / 100f, 0), tint, Vector2.zero);
        for (int i = 1; i < points.Length / 2 - 1; i++) mesh.AddTriangle(start, start+i, start+i+1);
    }
}
