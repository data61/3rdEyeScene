﻿using System;

namespace Tes.Net
{
  /// <summary>
  /// Flags controlling object creation.
  /// </summary>
  [Flags]
  public enum ObjectFlag : ushort
  {
    /// <summary>
    /// Null flag.
    /// </summary>
    None = 0,
    /// <summary>
    /// Shape should be rendered using wireframe rendering.
    /// </summary>
    Wireframe = (1 << 0),
    /// <summary>
    /// Shape is transparent. Colour should include an appropriate alpha value.
    /// </summary>
    Transparent = (1 << 1),
    /// <summary>
    /// Shape used a two sided shader (triangle culling disabled).
    /// </summary>
    TwoSided = (1 << 2),
    /// <summary>
    /// Shape creation should replace any pre-exiting shape with the same object ID.
    /// </summary>
    /// <remarks>
    /// Normally duplicate shape creation messages are not allowed. This flag allows a duplicate shape ID
    /// (non-transient) by replacing the previous shape.
    /// </remarks>
    Replace = (1 << 3),
    /// <summary>
    /// Creating multiple shapes in one message.
    /// </summary>
    MultiShape = (1 << 4),
    /// <summary>
    /// User flags start here.
    /// </summary>
    User = (1 << 8)
  }
}
