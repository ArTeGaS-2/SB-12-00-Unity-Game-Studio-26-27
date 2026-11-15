using UnityEngine;
using UnityEngine.UI;

public sealed class MenuConsoleGraphic : MaskableGraphic
{
    public int mode;
    public AudioSource audioSource;
    readonly float[] samples = new float[128];
    VertexHelper mesh;
    float nextRefresh;

    void Update()
    {
        if(mode!=2 || Time.unscaledTime<nextRefresh)return;
        nextRefresh=Time.unscaledTime+0.06f;
        if(audioSource!=null && audioSource.isPlaying)audioSource.GetOutputData(samples,0);
        else System.Array.Clear(samples,0,samples.Length);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();mesh=vh;
        if(mode==1)
        {
            for(int i=0;i<10;i++)Box(i*10,15,10,45,new Color(i/9f,i/9f,i/9f,1));
            for(int i=0;i<10;i++)Box(i*10+1,7,8,2,new Color(0.65f,0.55f,0.38f));
            Line(0,72,100,72,0.4f,color);
            Line(50,0,50,100,0.35f,new Color(1f,0.80f,0.30f,0.5f));
            return;
        }
        Color grid=new Color(0.65f,0.42f,0.16f,0.20f);
        for(int i=1;i<10;i++){Box(i*10,0,0.15f,100,grid);Box(0,i*10,100,0.3f,grid);}
        if(mode==2)
        {
            for(int i=0;i<64;i++)
            {
                float pulse=Mathf.Clamp01(Mathf.Abs(samples[i*2])*6);
                float height=2+pulse*88;
                Box(i*100f/64f,50-height/2,0.75f,height,color);
            }
        }
        else
        {
            // Shelter silhouette, distance guides and a rotating scan marker.
            Line(0,18,100,18,0.5f,color);
            Line(0,18,38,44,0.3f,grid);Line(100,18,62,44,0.3f,grid);
            Box(29,18,42,45,new Color(0.12f,0.075f,0.025f));
            Line(29,18,29,63,0.5f,color);Line(71,18,71,63,0.5f,color);Line(29,63,71,63,0.5f,color);
            Box(41,18,18,34,new Color(0.028f,0.018f,0.008f));
            Line(41,52,59,52,0.5f,color);Line(41,18,41,52,0.5f,color);Line(59,18,59,52,0.5f,color);
            for(int i=0;i<3;i++){Box(21+i*24,69,10,4,color);Box(21+i*24,77,10,1,grid);}
            Box(48,29,4,11,color);Box(44,33,12,3,color);
            for(int i=0;i<5;i++){Box(3,24+i*12,4,0.6f,color);Box(93,24+i*12,4,0.6f,color);}
        }
    }

    void Box(float x,float y,float w,float h,Color tint)
    {
        int start=mesh.currentVertCount;Rect r=rectTransform.rect;
        mesh.AddVert(new Vector3(r.xMin+x*r.width/100,r.yMin+y*r.height/100),tint,Vector2.zero);
        mesh.AddVert(new Vector3(r.xMin+(x+w)*r.width/100,r.yMin+y*r.height/100),tint,Vector2.zero);
        mesh.AddVert(new Vector3(r.xMin+(x+w)*r.width/100,r.yMin+(y+h)*r.height/100),tint,Vector2.zero);
        mesh.AddVert(new Vector3(r.xMin+x*r.width/100,r.yMin+(y+h)*r.height/100),tint,Vector2.zero);
        mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
    }
    void Line(float x1,float y1,float x2,float y2,float width,Color tint)
    {
        var direction=new Vector2(x2-x1,y2-y1);var normal=new Vector2(-direction.y,direction.x).normalized*width/2;
        var points=new[]{new Vector2(x1,y1)+normal,new Vector2(x2,y2)+normal,new Vector2(x2,y2)-normal,new Vector2(x1,y1)-normal};
        int start=mesh.currentVertCount;Rect r=rectTransform.rect;
        foreach(var p in points)mesh.AddVert(new Vector3(r.xMin+p.x*r.width/100,r.yMin+p.y*r.height/100),tint,Vector2.zero);
        mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
    }
}
