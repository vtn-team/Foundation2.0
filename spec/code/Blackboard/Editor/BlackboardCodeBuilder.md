# BlackboardCodeBuilder仕様

## コードの運用仕様
- プロジェクト内から、「Blackboard」アトリビュートが付与されたパラメータを特定し、ルールに従った拡張処理を行う。
- この処理は、UnityのReload Domain直後に行われる。

## ルール
- Blackboardがついたパラメータをグローバルの監視対象とする
	- 監視対象を登録するpartial classを自動生成する
- Blackboardクラスを監視対象クラスのパラメータを含めて再出力する
- パラメータに変化がない場合はファイルの変更を行わない
- 個々の自動生成されるファイルは、元ファイルと対になるように生成する
	- {元クラスの名前}BlackboardParams.cs
	- どのような値をどのような命名で自動生成するかはBlackboard.csの仕様に準ずること


## 処理
コードの更新後に、自動で`BlackboardCodeGenerator.GenerateCode();`を実行する


## 型名解決とusing句の自動生成
- アセンブリ参照がある型（例：`System.Collections.Generic.List<T>`）は、ILコード形式ではなく短縮型名を使用
- 各フィールドの型から必要なnamespaceを自動収集し、using句として生成コードに追加
- ジェネリック型（`List<T>`, `Dictionary<K,V>`等）の型引数も再帰的に処理
- 配列型の要素型についても同様に処理
- Enum型についてもフルアセンブリ名ではなく型名のみを使用
- 基本型（int, float, string, bool等）やUnityEngine名前空間の型は既定のusing句に含まれるため除外

