// Copyright (c) 2021 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using UnityEditor;

[InitializeOnLoad]
class UnityEditorStartup
{
  static UnityEditorStartup()
  {
    BuildPlayerWindow.RegisterBuildPlayerHandler(
      new System.Action<BuildPlayerOptions>(buildPlayerOptions =>
      {
        CreateAssetBundles.BuildAllAssetBundles();
        BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(buildPlayerOptions);
      })
    );
  }
}
