using UnityEngine;
using UnityEngine.UI;

/// <summary>Small, sharp equipment pictograms using the terminal's existing palette.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class MenuEquipmentIcon : MaskableGraphic
{
    public int item;
    VertexHelper mesh;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        mesh = vh;
        Color dark = new Color(0.065f, 0.042f, 0.024f, color.a);
        if (item == 0)
        {
            Box(30,76,40,12,color);
            Box(20,18,60,56,color);
            Box(26,24,48,44,dark);
            for (int i=0;i<4;i++) Box(31,30+i*9,38,3,color);
            Box(27,12,46,5,color);
        }
        else if (item == 1)
        {
            Box(35,73,30,12,color);
            Box(40,73,20,7,dark);
            Box(14,20,72,51,color);
            Box(20,26,60,39,dark);
            Box(45,32,10,27,color);
            Box(36,40,28,10,color);
        }
        else if (item == 2)
        {
            Box(39,80,22,8,color);
            Box(25,13,50,65,color);
            Box(31,19,38,53,dark);
            for (int i=0;i<3;i++) Box(36,25+i*15,28,10,color);
        }
        else
        {
            Polygon(color,25,83,75,83,88,64,80,27,50,10,20,27,12,64);
            Polygon(dark,29,75,71,75,79,62,73,33,50,20,27,33,21,62);
            Box(47,31,6,36,color);
            Box(30,58,40,4,color);
        }
    }

    void Box(float x,float y,float w,float h,Color tint)
    {
        Polygon(tint,x,y,x+w,y,x+w,y+h,x,y+h);
    }

    void Polygon(Color tint,params float[] points)
    {
        Rect r=rectTransform.rect;
        int start=mesh.currentVertCount;
        for(int i=0;i<points.Length;i+=2)
            mesh.AddVert(new Vector3(r.xMin+points[i]*r.width/100f,r.yMin+points[i+1]*r.height/100f,0),tint,Vector2.zero);
        for(int i=1;i<points.Length/2-1;i++) mesh.AddTriangle(start,start+i,start+i+1);
    }
}
