﻿using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//鼠标点击事件参数类
public class TileClickEventArgs : EventArgs
{
    public int MouseButton;//0左键，1右键
    public Tile Tile;

    public TileClickEventArgs(int mouseButton, Tile tile)
    {
        this.MouseButton = mouseButton;
        this.Tile = tile;
    }
}



/// <summary>
/// 用于描述一个关卡地图的状态
/// </summary>
public class Map : MonoBehaviour {
    #region 常量

    public int RowCount = 8; //行数
    public int ColumnCount = 12;//列数
    #endregion
    #region 事件

    public event EventHandler<TileClickEventArgs> OnTileClick;
    #endregion

    #region 字段
    //地图作为3D对象，单位是米
    private float MapWidth;//地图宽
    private float MapHeight;//地图高
   

    private float TileWidth;//格子宽
    private float TileHeight;//格子高

    public Level m_level;//关卡数据

  

    

   
    List<Tile> m_grid = new List<Tile>();//格子集合
     List<Tile> m_road = new List<Tile>();//路径引用

    public bool DrawGizmos = true; //是否绘制网格
    #endregion

    #region 属性

    public Level Level
    {
        get { return m_level; }
    }
    public string BackgroundImage
    {
        set
        {
            SpriteRenderer render = transform.Find("Background").GetComponent<SpriteRenderer>();
            StartCoroutine(Tools.LoadImage(value, render));
        }
    }
    public string RoadImage
    {
        set
        {
            SpriteRenderer render = transform.Find("Road").GetComponent<SpriteRenderer>();
            StartCoroutine(Tools.LoadImage(value, render));
        }
    }
    public Rect MapRect
    {
        get { return new Rect(-MapWidth / 2, -MapHeight / 2, MapWidth, MapHeight); }
    }
    public List<Tile> Grid
    {
        get { return m_grid; }
    }

    public List<Tile> Road
    {
        get { return m_road; }
    }

    public Vector3[] Path
    {
        get
        {
            List<Vector3> m_path = new List<Vector3>();
            for (int i = 0; i < m_road.Count; i++)
            {
                Tile t = m_road[i];
                Vector3 point = GetPosition(t);
                m_path.Add(point);
            }
            return m_path.ToArray();
        }
    }
    #endregion

    #region 方法

    public void LoadLevel(Level level)
    {
        //清除当前状态
        Clear();
        //保存
        this.m_level = level;
        //设置图片
        this.BackgroundImage = "file://" + Consts.MapDir + level.Background;
        this.RoadImage = "file://" + Consts.MapDir + level.Road;
        //寻路路径
        for (int i = 0; i < level.Path.Count; i++)
        {
            Point p = level.Path[i];
            Tile t = GetTile(p.X, p.Y);
            m_road.Add(t);
        }
        //炮塔空地
        for (int i = 0; i < level.Holder.Count; i++)
        {
            Point p = level.Holder[i];
            Tile t = GetTile(p.X, p.Y);
            t.CanHold = true;
        }

    }
    /// <summary>
    /// 清除塔位信息
    /// </summary>
    public void ClearHolder()
    {
        foreach (Tile t in m_grid)
        {
            if (t.CanHold)
            {
                t.CanHold = false;
            }
         
        }
    }
    /// <summary>
    /// 清除寻路格子
    /// </summary>
    public void ClearRoad()
    {
        m_road.Clear();
    }
    /// <summary>
    /// 清除所有信息
    /// </summary>
    public void Clear()
    {
        m_level = null;
        ClearHolder();
        ClearRoad();
    }
    #endregion

    #region Unity回调
    /// <summary>
    /// 只在运行期起作用
    /// </summary>
    void Awake()
    {
        //计算地图和格子大小
        CalcalateSize();
        //创建所有的格子
        for (int i = 0; i < RowCount; i++)
        {
            for (int j = 0; j < ColumnCount; j++)
            {
                m_grid.Add(new Tile(j,i));
            }
        }
        //监听鼠标点击事件
       
        OnTileClick += Map_OnTileClick;
    }

    void Update()
    {
        //鼠标左键检测
        if (Input.GetMouseButtonDown(0))
        {
            Tile t = GetTileUnderMouse();
            if (t != null)
            {
                //触发鼠标左键点击事件
                TileClickEventArgs e = new TileClickEventArgs(0,t);
                if (OnTileClick != null)
                {
                    OnTileClick(this, e);
                }
            }
        }
        //鼠标右键检测
        if (Input.GetMouseButtonDown(1))
        {
            Tile t = GetTileUnderMouse();
            if (t != null)
            {
                //触发鼠标右键点击事件
               TileClickEventArgs e = new TileClickEventArgs(1,t);
                if (OnTileClick != null)
                {
                    OnTileClick(this, e);
                } 
            }
        }
    }
    /// <summary>
    /// 只在编辑器里起作用
    /// </summary>
    void OnDrawGizmos()
    {
        if(!DrawGizmos)
            return;

        //计算地图和格子大小
        CalcalateSize();

        //绘制格子
        Gizmos.color = Color.green;

        //绘制行
        for (int row = 0; row <= RowCount; row++)
        {
            Vector2 from = new Vector2(-MapWidth/2,-MapHeight/2+row*TileHeight);
            Vector2 to = new Vector2(-MapWidth/2+MapWidth,-MapHeight/2+row*TileHeight);
            Gizmos.DrawLine(from,to);
        }

        //绘制列
        for (int col = 0; col<=ColumnCount; col++)
        {
            Vector2 from = new Vector2(-MapWidth/2+col*TileWidth,MapHeight/2);
            Vector2 to = new Vector2(-MapWidth/2+col*TileWidth,-MapHeight/2);
            Gizmos.DrawLine(from,to);
        }
        //绘制格子
        foreach (Tile t in m_grid)
        {
            if (t.CanHold)
            {
                Vector3 pos = GetPosition(t);
                Gizmos.DrawIcon(pos, "holder.png", true);
            }
        }
        Gizmos.color = Color.red;
        for (int i = 0; i < m_road.Count; i++)
        {
            //起点
            if (i == 0)
            {
                Gizmos.DrawIcon(GetPosition(m_road[i]), "start.png", true);

            }
            //终点
            if (m_road.Count > 1 && i == m_road.Count - 1)
            {
                Gizmos.DrawIcon(GetPosition(m_road[i]), "end.png", true);
            }
            //红色的连线
            if (m_road.Count > 1 && i != 0)
            {
                Vector3 from = GetPosition(m_road[i - 1]);
                Vector3 to = GetPosition(m_road[i]);
                Gizmos.DrawLine(from, to);
            }
        }
    }
    #endregion

    #region 事件回调

    void Map_OnTileClick(object sender, TileClickEventArgs e)
    {
        if (gameObject.scene.name != "LevelBuilder") 
            return;
        if (Level == null)
        {
            return;
        }
        //处理放塔操作
        if (e.MouseButton == 0 && !m_road.Contains(e.Tile))
        {
            e.Tile.CanHold = !e.Tile.CanHold;
            //
            Debug.Log("地图放塔");
        }
        //处理寻路点操作
        if (e.MouseButton == 1 && !e.Tile.CanHold)
        {
            if (m_road.Contains(e.Tile))
                m_road.Remove(e.Tile);
            else
            {
                m_road.Add(e.Tile);
                
            }
        }
        
    }
    #endregion

    #region 帮助方法
    /// <summary>
    /// 计算地图大小，格子大小
    /// </summary>
    void CalcalateSize()
    {
        //Screen.width 像素大小
        Vector3 leftDown = new Vector3(0,0);
        Vector3 rightUp = new Vector3(1,1);

        Vector3 p1 = Camera.main.ViewportToWorldPoint(leftDown);
        Vector3 p2 = Camera.main.ViewportToWorldPoint(rightUp);

        MapWidth =Math.Abs (p2.x - p1.x);
        MapHeight = Math.Abs(p2.y - p1.y);

        TileWidth = MapWidth/ColumnCount;
        TileHeight = MapHeight/RowCount;
    }
    /// <summary>
    /// 获取格子中心点所在的世界坐标
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
  public  Vector3 GetPosition(Tile t)
    {
        return new Vector3(-MapWidth/2+(t.X+0.5f)*TileWidth,-MapHeight/2+(t.Y+0.5f)*TileHeight,0  );
    }
    /// <summary>
    /// 根据格子索引号获得格子
    /// </summary>
    /// <param name="tileX"></param>
    /// <param name="tileY"></param>
    /// <returns></returns>
   public  Tile GetTile(int tileX, int tileY)
    {
        int index = tileX + tileY*ColumnCount;
        if (index < 0 || index >= m_grid.Count)
            throw new IndexOutOfRangeException("格子索引越界");
        return m_grid[index];
    }

    public Tile GetTile(Vector3 worldPos)
    {
        int tileX = (int)((worldPos.x + MapWidth / 2) / TileWidth);
        int tileY = (int)((worldPos.y + MapHeight / 2) / TileHeight);
        return GetTile(tileX, tileY);
    }
    /// <summary>
    /// 获取鼠标下面的格子
    /// </summary>
    /// <returns></returns>
    Tile GetTileUnderMouse()
    {
        Vector2 worldPos = GetWorldPosition();
       
        return GetTile(worldPos);
    }
    /// <summary>
    /// 获取鼠标所在的位置的世界坐标
    /// </summary>
    /// <returns></returns>
    Vector3 GetWorldPosition()
    {
        Vector3 viewPos = Camera.main.ScreenToViewportPoint(Input.mousePosition);
        Vector3 worldPos = Camera.main.ViewportToWorldPoint(viewPos);
        return worldPos;
    }
    #endregion
}
