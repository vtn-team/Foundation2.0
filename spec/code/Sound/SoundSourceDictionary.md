# SoundSourceDictionaryクラス設計

## 実装
- ScriptableObjectを継承する


# 概要
- ゲーム中に使用するAudioClipの情報をまとめる
- 将来的な実装
	- Addressablesのパスを設定できる


# 処理フロー
## 生成時
この処理は、シーン読み出し時にSceneLoaderからフックされる
1. soundDicListをDictionary<int, SoundDicItem>の型で展開し、アクセスしやすくする。
	1. 外部インタフェースからのアクセス時はDictionaryで行う。


# 変数
- soundDicList: SoundDicItemのリスト配列

## SoundDicItem
- ID: サウンドのENUMID(自動設定)。1から始まる。
- Block: シーン名
- keyName: アクセスに使用する名前
- audioClip: 音声ファイルの参照
- limit: 同時再生数
- priority: 優先度


# 外部インタフェース
- GetPrefab: キーに対応するPrefabを返す。
	- 再生していないAudioSourceを返却する
	- すべてのAudioSourceが再生中の場合、最も再生時間が古いものを返却する


# エディタ拡張
- CreateSoundEnumKeyList: ボタンを押すとキー配列を順番に並べた列挙型を生成する。0は必ずINVALIDとする。
- アサインされているAudioClipをサンプル再生できるようにする
	- AudioSourceを動的生成する

# デバッグ
1. UnityEditorから再生される場合は、開いているシーン名を取得して読み込みを行う
	1. これはシーン再生開始時に行い、すでに読み込まれていれば何もしない