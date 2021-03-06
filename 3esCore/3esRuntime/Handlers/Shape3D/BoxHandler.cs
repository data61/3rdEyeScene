﻿using Tes.Net;
using UnityEngine;

namespace Tes.Handlers.Shape3D
{
  /// <summary>
  /// Handles box shapes.
  /// </summary>
  public class BoxHandler : ShapeHandler
  {
    /// <summary>
    /// Create the shape handler.
    /// </summary>
    public BoxHandler()
    {
      SolidMesh = Tes.Tessellate.Box.Solid();
      WireframeMesh = Tes.Tessellate.Box.Wireframe();
    }

    public override void Initialise(GameObject root, GameObject serverRoot, Runtime.MaterialLibrary materials)
    {
      base.Initialise(root, serverRoot, materials);
    }

    /// <summary>
    /// Handler name.
    /// </summary>
    public override string Name { get { return "Box"; } }

    /// <summary>
    /// <see cref="ShapeID.Box"/>
    /// </summary>
    public override ushort RoutingID { get { return (ushort)Tes.Net.ShapeID.Box; } }
  }
}
