using System.Collections.Generic;
using UnityEngine;

namespace MantisLODEditor;

public class Lod_Mesh_Table
{
	public Mesh origin_mesh;

	public List<Component> containers;

	public Lod_Mesh[] lod_meshes;
}
