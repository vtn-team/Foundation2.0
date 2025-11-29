@echo off
REM ローカル環境固有の変更をgitで無視するバッチファイル
REM 実行すると、以下のファイルのローカル変更がgit statusに表示されなくなります
REM   - FontAsset (SDF*.asset): 動的に生成されるグリフテーブル
REM   - EditorUserSettings.asset: Unity Editorの個人設定
REM
REM 解除するには: script\track-changes.bat を実行

cd /d "%~dp0..\unity"

echo ローカル環境固有ファイルの変更をgitで無視します...
echo.

echo [FontAsset]
for %%f in ("Assets\Font\*SDF*.asset") do (
    echo   %%f
    git update-index --assume-unchanged "%%f"
)

echo.
echo [UserSettings]
git update-index --assume-unchanged "UserSettings\EditorUserSettings.asset"
echo   UserSettings\EditorUserSettings.asset

echo.
echo [BuildState]
git update-index --assume-unchanged "Assets\Scripts\BaseSystem\Dynamic\BuildState.cs"
echo   Assets\Scripts\BaseSystem\Dynamic\BuildState.cs

echo.
echo [AddressableAssets]
git update-index --assume-unchanged "Assets\AddressableAssetsData\AddressableAssetSettings.asset"
echo   Assets\AddressableAssetsData\AddressableAssetSettings.asset

echo.
echo [URP Settings]
git update-index --assume-unchanged "Assets\Settings\URP-Balanced.asset"
echo   Assets\Settings\URP-Balanced.asset
git update-index --assume-unchanged "Assets\Settings\URP-HighFidelity.asset"
echo   Assets\Settings\URP-HighFidelity.asset
git update-index --assume-unchanged "Assets\Settings\URP-Performant.asset"
echo   Assets\Settings\URP-Performant.asset

echo.
echo 完了しました。上記ファイルの変更はgit statusに表示されなくなりました。
echo 元に戻すには script\track-changes.bat を実行してください。
pause
