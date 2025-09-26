using UnityEngine;
using QFramework;
using System;
using MMORPG.Model;
using MMORPG.Tool;

namespace MMORPG.UI
{
    public enum ChatMessageType
    {
        General,
        World,
        Map,
        Group
    }

    public partial class UIChatItem : ViewController
    {
		void Start()
		{
			// Code Here
		}

        public void Setup(
            DateTime sendTime,
            string characterName,
            ChatMessageType type,
            string message,
            Color messageColor,
            bool isComposite)
        {
            var messageColorHex = messageColor.ToHex();

            if (isComposite)
            {
                string typeStr = type switch
                {
                    ChatMessageType.General => "General",
                    ChatMessageType.World => "World",
                    ChatMessageType.Group => "Group",
                    ChatMessageType.Map => "Map",
                    _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
                };

                TextMessage.SetText("<b>" +
                                    $"<color={messageColorHex}>[{sendTime:HH:mm:ss}][{typeStr}]</color>" +
                                    $"<color=#b59e8aff>[{characterName}]</color>" +
                                    "</b>" +
                                    $":{message}");
            }
            else
            {
                TextMessage.SetText("<b>" +
                                    $"<color={messageColorHex}>[{sendTime:HH:mm:ss}]</color>" +
                                    $"<color=#b59e8aff>[{characterName}]</color>" +
                                    "</b>" +
                                    $":{message}");
            }
        }
    }
}
