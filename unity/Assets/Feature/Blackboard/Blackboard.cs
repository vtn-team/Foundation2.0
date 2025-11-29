using System;
using System.Collections.Generic;
using UnityEngine;
using R3;

/// <summary>
/// グローバルに参照されるパラメータを安全に参照するシングルトン
/// partial classで自動生成されるプロパティと結合される
/// </summary>
public partial class Blackboard : MonoBehaviour
{
    private static Blackboard _instance;

    /// <summary>
    /// シングルトンインスタンス
    /// </summary>
    public static Blackboard Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("[Blackboard]");
                _instance = go.AddComponent<Blackboard>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    /// <summary>
    /// カテゴリごとの購読解除用Disposable
    /// </summary>
    private Dictionary<string, CompositeDisposable> _categoryDisposables = new Dictionary<string, CompositeDisposable>();

    /// <summary>
    /// 登録されているReactivePropertyの情報（デバッグ用）
    /// </summary>
    private Dictionary<string, object> _registeredProperties = new Dictionary<string, object>();

    /// <summary>
    /// 購読情報（デバッグ用）
    /// </summary>
    private Dictionary<string, int> _subscriptionCounts = new Dictionary<string, int>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// ゲーム開始時に初期化
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        // インスタンスを事前作成
        var _ = Instance;
    }

    /// <summary>
    /// カテゴリ用のDisposableを取得または作成
    /// </summary>
    private CompositeDisposable GetOrCreateCategoryDisposable(string category)
    {
        if (string.IsNullOrEmpty(category))
        {
            category = "Default";
        }

        if (!_categoryDisposables.TryGetValue(category, out var disposable))
        {
            disposable = new CompositeDisposable();
            _categoryDisposables[category] = disposable;
        }

        return disposable;
    }

    /// <summary>
    /// 値を登録
    /// </summary>
    /// <typeparam name="T">値の型</typeparam>
    /// <param name="propertyName">プロパティ名</param>
    /// <param name="property">ReactiveProperty</param>
    /// <param name="category">カテゴリ名</param>
    public void Register<T>(string propertyName, ReactiveProperty<T> property, string category = "")
    {
        _registeredProperties[propertyName] = property;

        var disposable = GetOrCreateCategoryDisposable(category);

        // Disposeされたときに登録情報を削除
        Disposable.Create(() =>
        {
            _registeredProperties.Remove(propertyName);
        }).AddTo(disposable);
    }

    /// <summary>
    /// 値の更新を購読
    /// </summary>
    /// <typeparam name="T">値の型</typeparam>
    /// <param name="propertyName">プロパティ名</param>
    /// <param name="property">ReactiveProperty</param>
    /// <param name="onNext">値変更時のコールバック</param>
    /// <param name="category">カテゴリ名</param>
    /// <returns>購読解除用のDisposable</returns>
    public IDisposable Subscribe<T>(string propertyName, ReactiveProperty<T> property, Action<T> onNext, string category = "")
    {
        if (!_subscriptionCounts.ContainsKey(propertyName))
        {
            _subscriptionCounts[propertyName] = 0;
        }
        _subscriptionCounts[propertyName]++;

        var subscription = property.Subscribe(onNext);

        var disposable = GetOrCreateCategoryDisposable(category);

        // ラップしたDisposableを返す
        var wrappedDisposable = Disposable.Create(() =>
        {
            subscription.Dispose();
            if (_subscriptionCounts.ContainsKey(propertyName))
            {
                _subscriptionCounts[propertyName]--;
                if (_subscriptionCounts[propertyName] <= 0)
                {
                    _subscriptionCounts.Remove(propertyName);
                }
            }
        });

        wrappedDisposable.AddTo(disposable);

        return wrappedDisposable;
    }

    /// <summary>
    /// 値の購読を解除（Describeは古い命名。Unsubscribeを使用推奨）
    /// </summary>
    /// <param name="disposable">購読時に返されたDisposable</param>
    public void Describe(IDisposable disposable)
    {
        Unsubscribe(disposable);
    }

    /// <summary>
    /// 値の購読を解除
    /// </summary>
    /// <param name="disposable">購読時に返されたDisposable</param>
    public void Unsubscribe(IDisposable disposable)
    {
        disposable?.Dispose();
    }

    /// <summary>
    /// カテゴリを指定して値の登録と購読を全解除
    /// </summary>
    /// <param name="category">カテゴリ名</param>
    public void Release(string category)
    {
        if (string.IsNullOrEmpty(category))
        {
            category = "Default";
        }

        if (_categoryDisposables.TryGetValue(category, out var disposable))
        {
            disposable.Dispose();
            _categoryDisposables.Remove(category);
        }
    }

    /// <summary>
    /// 全ての登録と購読を解除
    /// </summary>
    public void ReleaseAll()
    {
        foreach (var disposable in _categoryDisposables.Values)
        {
            disposable.Dispose();
        }
        _categoryDisposables.Clear();
        _registeredProperties.Clear();
        _subscriptionCounts.Clear();
    }

    /// <summary>
    /// 登録されているプロパティ名を取得（デバッグ用）
    /// </summary>
    public IEnumerable<string> GetRegisteredPropertyNames()
    {
        return _registeredProperties.Keys;
    }

    /// <summary>
    /// 購読数を取得（デバッグ用）
    /// </summary>
    public int GetSubscriptionCount(string propertyName)
    {
        return _subscriptionCounts.TryGetValue(propertyName, out var count) ? count : 0;
    }

    /// <summary>
    /// デバッグ情報を文字列で取得
    /// </summary>
    public string GetDebugInfo()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== Blackboard Debug Info ===");

        sb.AppendLine("\n[Registered Properties]");
        foreach (var kvp in _registeredProperties)
        {
            var subCount = GetSubscriptionCount(kvp.Key);
            sb.AppendLine($"  {kvp.Key}: {subCount} subscribers");
        }

        sb.AppendLine("\n[Categories]");
        foreach (var category in _categoryDisposables.Keys)
        {
            sb.AppendLine($"  {category}");
        }

        return sb.ToString();
    }

    private void OnDestroy()
    {
        ReleaseAll();
    }
}
