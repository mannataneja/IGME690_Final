using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Polygon
{
    public List<int>     m_Vertices;       
    public List<Vector2> m_UVs; 
    public List<Polygon> m_Neighbors;
    public Color32       m_Color;
    public bool          m_SmoothNormals;

    public Polygon(int a, int b, int c)
    {
        m_Vertices      = new List<int>() { a, b, c };
        m_Neighbors     = new List<Polygon>();
        m_UVs           = new List<Vector2>() { Vector2.zero, Vector2.zero, Vector2.zero };
        m_SmoothNormals = true;

        m_Color = new Color32(255, 0, 255, 255);
    }

    /// <summary>
    /// Calculate if two polygons share an edge
    /// </summary>
    public bool IsNeighborOf(Polygon other_poly)
    {
        int shared_vertices = 0;
        foreach (int vertex in m_Vertices)
        {
            if (other_poly.m_Vertices.Contains(vertex))
                shared_vertices++;
        }

        //Negighbors will share 2 vertices
        return shared_vertices == 2;
    }

    public void ReplaceNeighbor(Polygon oldNeighbor, Polygon newNeighbor)
    {
        for(int i = 0; i < m_Neighbors.Count; i++)
        {
            if(oldNeighbor == m_Neighbors[i])
            {
                m_Neighbors[i] = newNeighbor;
                return;
            }
        }
    }
}


public class PolySet : HashSet<Polygon>
{
    public PolySet() {}
    public PolySet(PolySet source) : base(source) {}


    // If this PolySet was created by stitching existing Polys, then we store the index of the last original vertex before we did the stitching
    public int m_StitchedVertexThreshold = -1;

    // Calculate the edges that surround the set of polygons

    public EdgeSet CreateEdgeSet()
    {
        EdgeSet edgeSet = new EdgeSet();

        foreach (Polygon poly in this)
        {
            foreach (Polygon neighbor in poly.m_Neighbors)
            {
                if (this.Contains(neighbor))
                    continue;

                // If the neighbor is not on the PolySet, then the edge shared with neighbor is one of the edges on the PolySet
                Edge edge = new Edge(poly, neighbor);
                edgeSet.Add(edge);
            }
        }
        return edgeSet;
    }

    // Remove any poly gone from the set that borders the edge of the set, including ones that just touch the edge with a single vertex
    public PolySet RemoveEdges()
    {
        var newSet = new PolySet();

        var edgeSet = CreateEdgeSet();

        var edgeVertices = edgeSet.GetUniqueVertices();

        foreach(Polygon poly in this)
        {
            bool polyTouchesEdge = false;

            for(int i = 0; i < 3; i++)
            {
                if(edgeVertices.Contains(poly.m_Vertices[i]))
                {
                    polyTouchesEdge = true;
                    break;
                }
            }

            if (polyTouchesEdge)
                continue;

            newSet.Add(poly);
        }

        return newSet;
    }

    /// <summary>
    /// Calculate a list of the vertex indices used by these Polygons with no duplicates.
    /// </summary>

    public List<int> GetUniqueVertices()
    {
        List<int> verts = new List<int>();
        foreach (Polygon poly in this)
        {
            foreach (int vert in poly.m_Vertices)
            {
                if (!verts.Contains(vert))
                    verts.Add(vert);
            }
        }
        return verts;
    }

    public void ApplyAmbientOcclusionTerm(float AOForOriginalVerts, float AOForNewVerts)
    {
        foreach (Polygon poly in this)
        {
            for (int i = 0; i < 3; i++)
            {
                float ambientOcclusionTerm = (poly.m_Vertices[i] > m_StitchedVertexThreshold) ? AOForNewVerts : AOForOriginalVerts;

                Vector2 uv = poly.m_UVs[i];
                uv.y = ambientOcclusionTerm;
                poly.m_UVs[i] = uv;
            }
        }
    }

    /// <summary>
    /// Apply Color to all polygons 
    /// </summary>
    /// <param name="c"></param>
    public void ApplyColor(Color32 c)
    {
        foreach (Polygon poly in this)
            poly.m_Color = c;
    }
}
