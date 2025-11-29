@echo off
REM ローカル環境固有ファイルの変更を再びgitで追跡するバッチファイル
REM ignore-changes.bat で無視設定したものを解除します

cd /d "%~dp0..\unity"

echo ローカル環境固有ファイルの変更をgitで再追跡します...
echo.

echo [FontAsset]
for %%f in ("Assets\Font\*SDF*.asset") do (
    echo   %%f
    git update-index --no-assume-unchanged "%%f"
)

echo.
echo [UserSettings]
git update-index --no-assume-unchanged "UserSettings\EditorUserSettings.asset"
echo   UserSettings\EditorUserSettings.asset

echo.
echo [BuildState]
git update-index --no-assume-unchanged "Assets\Scripts\BaseSystem\Dynamic\BuildState.cs"
echo   Assets\Scripts\BaseSystem\Dynamic\BuildState.cs

echo.
echo [AddressableAssets]
git update-index --no-assume-unchanged "Assets\AddressableAssetsData\AddressableAssetSettings.asset"
echo   Assets\AddressableAssetsData\AddressableAssetSettings.asset

echo.
echo [URP Settings]
git update-index --no-assume-unchanged "Assets\Settings\URP-Balanced.asset"
echo   Assets\Settings\URP-Balanced.asset
git update-index --no-assume-unchanged "Assets\Settings\URP-HighFidelity.asset"
echo   Assets\Settings\URP-HighFidelity.asset
git update-index --no-assume-unchanged "Assets\Settings\URP-Performant.asset"
echo   Assets\Settings\URP-Performant.asset

echo.
echo 完了しました。上記ファイルの変更がgit statusに表示されるようになりました。
pause
