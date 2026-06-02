using UnityEngine;
using UnityEngine.EventSystems;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace CozyLifeSim.UI
{
    [RequireComponent(typeof(Collider2D))]
    public class CozyNPCWidget : MonoBehaviour
    {
        [System.Serializable]
        public class NpcDialogueLine
        {
            [TextArea(3, 5)] public string Line;
        }

        [System.Serializable]
        public class NpcDialogueData
        {
            public string NpcName;
            public List<NpcDialogueLine> Dialogues = new List<NpcDialogueLine>();
            public Sprite Portrait;
        }

        [SerializeField] private NpcDialogueData _npcData;
        private CozyDialoguePopup _dialoguePopup;

        private void Start()
        {
            if (_dialoguePopup == null)
            {
                _dialoguePopup = FindFirstObjectByType<CozyDialoguePopup>();
            }
        }

        private void OnMouseDown()
        {
            // Pointer Guard: block click neu chuot dang đè len UI (vi du shop button, close button)
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (_dialoguePopup != null && _npcData.Dialogues != null && _npcData.Dialogues.Count > 0)
            {
                int randomIndex = Random.Range(0, _npcData.Dialogues.Count);
                string randomDialogue = _npcData.Dialogues[randomIndex].Line;
                _dialoguePopup.ShowDialogue(_npcData.NpcName, randomDialogue, _npcData.Portrait).Forget();
            }
        }
    }
}
