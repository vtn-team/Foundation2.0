# PrefabDictionaryクラス設計

## 実装
- ScriptableObjectを継承する
- 1つしか存在しないアセットで、ゲーム内では実装としてシングルトンとして表現される


# 概要
- ゲーム中に使用するPrefabの情報をまとめる
	- キー： Prefabの参照を行う辞書を持つ
- 将来的な実装
	- Addressablesのパスを設定できる
	- Prefab生成時に必要なスクリプトがあればつける


# 処理フロー
## 生成時
prefabDicListをDictionary<string, GameObject>の型で展開し、アクセスしやすくする。
外部インタフェースからのアクセス時はDictionaryで行う。

# 変数
- prefabDicList: PrefabDicItemのリスト配列

## PrefabDicItem
- keyName: アクセスに使用する名前。
- prefab: Prefabの参照。
- Limit: [生成上限値]。


# 外部インタフェース
- GetPrefab: キーに対応するPrefabを返す


# エディタ拡張
## 読み取り系
- GetKeyList: キーをstringの配列で返す
- IsKeyRegistered: 指定したキーが登録されているかを確認する

## 登録・更新系
- RegisterPrefab: 新しいPrefabを登録する
	- 引数: key（キー）, prefab（Prefab参照）, limit（生成上限値、デフォルト10）
	- NOTE: キーが既に存在する場合は警告を出して何もしない
	- リストと辞書の両方に追加する
- UpdatePrefab: 既存のPrefabを更新する
	- 引数: key（キー）, prefab（新しいPrefab参照）, limit（生成上限値、デフォルト10）
	- NOTE: キーが存在しない場合は警告を出して何もしない
	- リストと辞書の両方を更新する
- AddList: PrefabDicItemをリストに追加する（既存メソッド）

## Attribute
### PrefabDictionaryFilterAttribute
特定のコンポーネントを持つPrefabのみをフィルタリングして表示するPropertyAttribute

**適用対象**: stringフィールド

**機能**:
- PrefabDictionaryから指定されたコンポーネントを持つPrefabのみをPopupで表示
- KeyPrefixによるキーのプレフィックスフィルタリング（オプション）
- 空の選択肢の表示/非表示設定（デフォルト: 表示）

**使用例**:
```csharp
// BasicEffectコンポーネントを持つPrefabのみを表示
[PrefabDictionaryFilter(typeof(BasicEffect))]
public string effectKey;

// IEffectインターフェースを実装したPrefabのみ、"Effect/"プレフィックス付きで表示
[PrefabDictionaryFilter(typeof(IEffect))]
[KeyPrefix = "Effect/"]
public string effectKey;
```

**プロパティ**:
- ComponentType: フィルタリングするコンポーネントの型（必須）
- KeyPrefix: キーのプレフィックスフィルタ（オプション）
- IncludeEmpty: 空の選択肢を含めるか（デフォルト: true）

**右クリックメニュー**:
- Refresh List: フィルタリストを再読み込み
- Select PrefabDictionary: PrefabDictionaryアセットを選択
- Select Prefab: 選択中のPrefabを選択

**実装**:
- Attribute: `PrefabDictionaryFilterAttribute.cs`
- PropertyDrawer: `PrefabDictionaryFilterAttributeDrawer.cs`