using System;
using System.Collections.Generic;
using System.Text;
using MMORPG.Common.Proto.Fight;

namespace MMORPG.Common.Tool
{
    public struct ErrorInfo
    {
        public Proto.Base.NetError Error;
        public string Description;
    }

    public static class Extension
    {
        public static ErrorInfo GetInfo(this Proto.Base.NetError error)
        {
            var info = new ErrorInfo() { Error = error };
            switch (error)
            {
                case Proto.Base.NetError.Success:
                    info.Description = "Request successful";
                    break;
                case Proto.Base.NetError.LoginConflict:
                    info.Description = "The current account has been logged in";
                    break;
                case Proto.Base.NetError.IncorrectUsernameOrPassword:
                    info.Description = "Incorrect username or password";
                    break;
                case Proto.Base.NetError.IllegalUsername:
                    info.Description = "Illegal username";
                    break;
                case Proto.Base.NetError.IllegalCharacterName:
                    info.Description = "Illegal character name";
                    break;
                case Proto.Base.NetError.RepeatUsername:
                    info.Description = "Username has been registered";
                    break;
                case Proto.Base.NetError.RepeatCharacterName:
                    info.Description = "The character name has been registered";
                    break;
                case Proto.Base.NetError.InvalidCharacter:
                    info.Description = "Invalid role";
                    break;
                case Proto.Base.NetError.InvalidMap:
                    info.Description = "Invalid map";
                    break;
                case Proto.Base.NetError.CharacterCreationLimitReached:
                    info.Description = "Character creation has reached the maximum limit!";
                    break;
                case Proto.Base.NetError.UnknowError:
                default:
                    info.Description = "unknown error";
                    break;
            }
            return info;
        }

        public static LinkedListNode<T>? FindIf<T>(this LinkedList<T> list, Func<T, bool> predicate)
        {
            var node = list.First;
            while (node != null)
            {
                if (predicate(node.Value))
                    return node;
                node = node.Next;
            }
            return node;
        }

        public static void RemoveIf<T>(this LinkedList<T> list, Func<T, bool> predicate)
        {
            list.Remove(list.FindIf(predicate));
        }

        public static string ToString(this CastInfo info)
        {
            return $"CastInfo:\"SkillId:{info.SkillId}|{info.CastTarget}:{info.CasterId}|\"";
        }
    }
}
