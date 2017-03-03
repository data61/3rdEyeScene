﻿using System.ComponentModel;
using UnityEngine;

public class RenderSettings : Settings
{
  private static RenderSettings _instance = new RenderSettings();
  public static RenderSettings Instance { get { return _instance; } }

  public RenderSettings()
  {
    Name = "Render";
  }

  [Browsable(true), Tooltip("Enable Eye-Dome-Lighting shader?")]
  public bool EdlShader
  {
    get { return PlayerPrefsX.GetBool("render.edl", true); }
    set { PlayerPrefsX.SetBool("render.edl", value); Notify("EdlShader"); }
  }

  [Browsable(true), SetRange(1, 10), Tooltip("The pixel search radius used in EDL calculations.")]
  public int EdlRadius
  {
    get { return PlayerPrefs.GetInt("render.edlRadius", 1); }
    set { PlayerPrefs.SetInt("render.edlRadius", value); Notify("EdlRadius"); }
  }

  [Browsable(true), SetRange(1, 1000), Tooltip("Exponential scaling for EDL shader.")]
  public float EdlExponentialScale
  {
    get { return PlayerPrefs.GetFloat("render.edlExponentialScale", 500); }
    set { PlayerPrefs.SetFloat("render.edlExponentialScale", value); Notify("EdlExponentialScale"); }
  }

  [Browsable(true), SetRange(1, 100), Tooltip("Linear scaling for EDL shader.")]
  public float EdlLinearScale
  {
    get { return PlayerPrefs.GetFloat("render.edlLinearScale", 10); }
    set { PlayerPrefs.SetFloat("render.edlLinearScale", value); Notify("EdlLinearScale"); }
  }

  /// <summary>
  /// Values for the <see cref="Background"/> property.
  /// </summary>
  public enum RenderBackground
  {
    /// <summary>
    /// Render the skybox.
    /// </summary>
    Skybox,
    Black,
    White,
    Red,
    Green,
    Blue,
    Yellow,
    Magenta,
    Cyan
  }

  public static Color32[] BackgroundColours { get { return _backgroundColours; } }
  private static Color32[] _backgroundColours = new Color32[]
  {
    new Color32(0, 0, 0, 0),  // Clear/skybox.
    new Color32(0, 0, 0, 255),  // Black
    new Color32(255, 255, 255, 255),  // White
    new Color32(255, 0, 0, 255),  // Red
    new Color32(0, 255, 0, 255),  // Green
    new Color32(0, 0, 255, 255),  // Blue
    new Color32(255, 255, 0, 255),  // Yellow
    new Color32(255, 0, 255, 255),  // Magenta
    new Color32(0, 255, 255, 255) // Cyan
  };

  [Browsable(true), Tooltip("Use a skybox for the background (true) or a flat colour (false).")]
  public RenderBackground Background
  {
    get
    {
      int intVal = PlayerPrefs.GetInt("render.background", 0);
      if (0 <= intVal || intVal <= (int)RenderBackground.Cyan)
      {
        return (RenderBackground)intVal;
      }
      Background = RenderBackground.Skybox;
      return RenderBackground.Skybox;
    }

    set
    {
      PlayerPrefs.SetInt("render.background", (int)value);
    }
  }
}