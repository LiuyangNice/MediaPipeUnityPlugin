// Copyright (c) 2021 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.IO;
using UnityEditor;
using UnityEngine;

public class CreateAssetBundles
{
  [MenuItem("Assets/Build AssetBundles")]
  internal static void BuildAllAssetBundles()
  {
    string assetBundleDirectory = Application.streamingAssetsPath;
    if (!Directory.Exists(assetBundleDirectory))
    {
      Directory.CreateDirectory(assetBundleDirectory);
    }
    BuildPipeline.BuildAssetBundles(assetBundleDirectory,
                                    BuildAssetBundleOptions.None,
                                    EditorUserBuildSettings.activeBuildTarget);
  }
}
