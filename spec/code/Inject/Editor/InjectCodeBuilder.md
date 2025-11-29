# InjectCodeBuilder仕様

## コードの運用仕様
- プロジェクト内から、「Inject」アトリビュートが付与されたクラスを特定し、ルールに従った拡張処理を行う。
- この処理は、UnityのReload Domain直後に行われる。

## ルール
- Injectがついたパラメータの注入設定(InjectParamListクラス)
- 設定ファイルを選択可能にする(ParamInjectSettingsクラス)
- プロジェクト内のInjectがついている変数をリストアップして設定ファイルとして自動生成して出力
	- 値の注入(正確には取得して代入する)用のpartial classを自動生成する
	- Injectするパラメータに変化がない場合はファイルの変更を行わない。
- 個々の自動生成されるファイルは、元ファイルと対になるように生成する
	- 名前のルールは以下
	- 注入対象と対になるクラスは、{元クラスの名前}InjectParams.cs
	- InjectParamListの中身は、InjectParamListParams.cs
	- InjectParamListの中身はすべてプロパティでアクセス可能とし、注入対象のクラスから対象の変数名と同じ名前で直にアクセスできる想定(読み取りのみ)
	- 値は、InjectSystem→ParamInjectSettings→- InjectParamListと経由してアクセスする。InjectSystemはシングルトンである。

## 処理
コードの更新後に、自動で`DICodeGenerator.GenerateInjectionCode();`を実行する

## 型名解決とusing句の自動生成
- アセンブリ参照がある型（例：`System.Collections.Generic.List<T>`）は、ILコード形式ではなく短縮型名を使用
- 各フィールドの型から必要なnamespaceを自動収集し、using句として生成コードに追加
- ジェネリック型（`List<T>`, `Dictionary<K,V>`等）の型引数も再帰的に処理
- 配列型の要素型についても同様に処理
- Enum型についてもフルアセンブリ名ではなく型名のみを使用
- 基本型（int, float, string, bool等）やUnityEngine名前空間の型は既定のusing句に含まれるため除外


## InjectParamList
クラスが自動生成される。アセットは手動生成である。
本体はpartial classで、以下をペアとする変数定義が自動生成される。
- 参照用のプロパティ。宣言名はアッパーキャメル。
- ユーザが入力可能な、SerializeFieldされる変数。同名で変数名の先頭にアンダースコアをつける。
変数のデフォルト値は、注入予定のクラスで設定されているデフォルト値を踏襲すること

## InjectParamListEditor
InjectParamInfoを参照して表示を成形するエディタ拡張クラスがある。
エディタ拡張の仕様は以下である。
- InjectParamInfoは`Assets/DataAsset/Params`にある
- 同じ名前を持つグループごとに分類し、Expandできるようにする。
- VarNameと対応する変数があることを前提とする(リフレクションから参照して存在確認は行うこと)
- 変数名に対応する日本語(ViewName)を表記する。


## InjectParamInfo
注入先の変数名の説明を記載するアセット。およびそのクラス。
Group、VarName、ViewNameの3つの変数を持つクラスParamInfoがあり、
InjectParamInfoはParamInfoのListをメンバに持つScriptableObjectクラス。


## ParamInjectSettings
`Assets/DataAsset/Params`直下に必ず存在する想定のアセットで、手動で生成する。
使用するInjectParamListを選択する。