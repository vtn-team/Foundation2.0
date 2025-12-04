#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Cysharp.Threading.Tasks;

namespace Melpomene
{
    /// <summary>
    /// Melpomeneチケットのコンテキストメニュー表示ユーティリティ
    /// NOTE: チケット右クリックメニューの表示を担当
    /// </summary>
    public static class MelpomeneGizmoDrawer
    {

        /// <summary>
        /// チケットのコンテキストメニューを表示
        /// </summary>
        public static void ShowTicketPopup(MelpomeneTicket ticket, Vector2 position)
        {
            var menu = new GenericMenu();

            menu.AddItem(new GUIContent($"#{ticket.issueNumber}: {ticket.title}"), false, null);
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Open in Browser"), false, () =>
            {
                if (!string.IsNullOrEmpty(ticket.issueUrl))
                {
                    Application.OpenURL(ticket.issueUrl);
                }
            });
            menu.AddItem(new GUIContent("Copy URL"), false, () =>
            {
                GUIUtility.systemCopyBuffer = ticket.issueUrl;
            });
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("View Details"), false, () =>
            {
                MelpomeneTicketDetailWindow.ShowWindow(ticket);
            });

            if (ticket.state == "open")
            {
                menu.AddItem(new GUIContent("Close Issue"), false, () =>
                {
                    if (EditorUtility.DisplayDialog("Close Issue", $"Close issue #{ticket.issueNumber}?", "Yes", "No"))
                    {
                        MelpomeneManager.Instance.CloseTicketAsync(ticket.issueNumber).Forget();
                    }
                });
            }

            menu.ShowAsContext();
        }
    }
}
#endif
