# ReleaseOverride

# 概要
- リリース時の各種デバッグ的な処理を再定義する
- リリース状態かどうかは、「RELEASE」のDefine Symbolがあるかどうかで確認する

# 処理
- Debug.Log、Debug.LogWarningの場合は、当該関数を何もしない関数として再定義し、コンパイラで処理ごと消されるようにする。

# 該当する処理
- Debug.Log
- Debug.LogWarning