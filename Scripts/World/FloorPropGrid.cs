using Godot;
using System.Collections.Generic;

namespace LastWord.World;

public partial class FloorPropGrid : Node
{
    [Export] public bool AutoConvert = true;
    [Export] public bool HideOriginals = true;
    [Export] public int MinimumInstances = 3;

    public override void _Ready()
    {
        if (!AutoConvert)
            return;

        ConvertRepeatedMeshes();
    }

    private void ConvertRepeatedMeshes()
    {
        Node root = GetParent() ?? GetTree().CurrentScene;
        if (root == null)
            return;

        var groups = new Dictionary<Mesh, List<MeshInstance3D>>();
        CollectMeshInstances(root, groups);

        foreach (var pair in groups)
        {
            Mesh mesh = pair.Key;
            List<MeshInstance3D> instances = pair.Value;
            if (mesh == null || instances.Count < MinimumInstances)
                continue;

            var multiMesh = new MultiMesh
            {
                Mesh = mesh,
                TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
                InstanceCount = instances.Count
            };

            for (int i = 0; i < instances.Count; i++)
            {
                multiMesh.SetInstanceTransform(i, instances[i].GlobalTransform);
                if (HideOriginals)
                    instances[i].Visible = false;
            }

            var batched = new MultiMeshInstance3D
            {
                Name = $"Batched_{mesh.ResourceName}",
                Multimesh = multiMesh
            };
            AddChild(batched);
        }
    }

    private void CollectMeshInstances(Node node, Dictionary<Mesh, List<MeshInstance3D>> groups)
    {
        if (node is MeshInstance3D meshInstance && meshInstance.Mesh != null && meshInstance.Visible)
        {
            if (!groups.TryGetValue(meshInstance.Mesh, out var list))
            {
                list = new List<MeshInstance3D>();
                groups[meshInstance.Mesh] = list;
            }
            list.Add(meshInstance);
        }

        foreach (Node child in node.GetChildren())
            CollectMeshInstances(child, groups);
    }
}
