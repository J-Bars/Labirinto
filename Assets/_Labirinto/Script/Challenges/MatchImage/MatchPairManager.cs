using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;
using System.Linq;
using Challenge;

namespace MatchPairGame
{
    public class MatchGameManager : MonoBehaviour
    {
        [Header("Game Elements")]
        [SerializeField] private GameObject draggableItemsPanel;
        [SerializeField] private GameObject slotPanel;
        [SerializeField] private GameObject draggableItemPrefab;
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private Button submitButton;
        [SerializeField] private TextMeshProUGUI challengeName;
        [SerializeField] private Timer gameTimer; // Reference to Timer

        private Dictionary<string, string> pairs = new Dictionary<string, string>();
        private int score = 0; // To track the score

        void Start()
        {
            LoadGameFromChallengeManager();
            submitButton.onClick.AddListener(CheckMatches);

            // Ensure gameTimer is assigned before subscribing to events
            if (gameTimer != null)
            {
                gameTimer.OnTimeEnd += CheckMatches;
            }
            else
            {
                Debug.LogError("Game Timer is not assigned!");
            }
        }

        public void LoadGameFromChallengeManager()
        {
            // Get the game JSON from the ChallengeManager
            TextAsset gameJson = ChallengeManager.Instance.CurrentChallengeJson;

            if (gameJson != null)
            {
                LoadGame(gameJson);
            }
            else
            {
                Debug.LogError("No game JSON found in ChallengeManager!");
            }
        }

        public void LoadGame(TextAsset gameJson)
        {
            if (gameJson != null)
            {
                string jsonText = gameJson.text;
                MatchData matchData = JsonConvert.DeserializeObject<MatchData>(jsonText);

                challengeName.text = matchData.challengeName;

                // Shuffle pairs
                var shuffledPairs = matchData.pairs.OrderBy(pair => Random.value).ToList();

                foreach (var pair in shuffledPairs)
                {
                    pairs[pair.item] = pair.pair;

                    // Create draggable item
                    GameObject draggableItem = Instantiate(draggableItemPrefab, draggableItemsPanel.transform);
                    draggableItem.GetComponentInChildren<TextMeshProUGUI>().text = pair.item;

                    // Assign the image for the draggable item
                    Image draggableItemImage = draggableItem.GetComponentInChildren<Image>();
                    if (draggableItemImage != null)
                    {
                        Sprite itemSprite = Resources.Load<Sprite>($"MatchPairRes/{pair.itemImage}");
                        if (itemSprite != null)
                        {
                            draggableItemImage.sprite = itemSprite;
                        }
                        else
                        {
                            Debug.LogWarning($"Image not found for draggable item: {pair.itemImage}");
                        }
                    }

                    // Create slot
                    GameObject slot = Instantiate(slotPrefab, slotPanel.transform);
                    slot.GetComponentInChildren<TextMeshProUGUI>().text = pair.pair;
                    slot.name = pair.pair; // Use pair as the slot name

                    // Assign the image for the slot
                    Image slotImage = slot.GetComponentInChildren<Image>();
                    if (slotImage != null)
                    {
                        Sprite slotSprite = Resources.Load<Sprite>($"MatchPairRes/{pair.slotImage}");
                        if (slotSprite != null)
                        {
                            slotImage.sprite = slotSprite;
                        }
                        else
                        {
                            Debug.LogWarning($"Image not found for slot: {pair.slotImage}");
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("Game JSON file is missing!");
            }
        }

        public void CheckMatches()
        {
            int matchedCount = 0;

            // Reset score
            score = 0;

            foreach (Transform slot in slotPanel.transform)
            {
                string expectedPair = slot.name;

                // Check if the slot has a draggable item
                if (slot.childCount > 1) // Ensures the slot contains a draggable item
                {
                    Transform draggableItem = slot.GetChild(1);
                    var textMesh = draggableItem.GetComponentInChildren<TextMeshProUGUI>();

                    if (textMesh == null)
                    {
                        Debug.LogWarning($"No TextMeshProUGUI component found in draggable item for slot: {slot.name}. Adding 0 score.");
                        score += 0; // Add 0 score
                        continue; // Skip further processing for this slot
                    }

                    string draggedItemName = textMesh.text;

                    if (pairs.ContainsKey(draggedItemName))
                    {
                        if (pairs[draggedItemName] == expectedPair)
                        {
                            Debug.Log($"Matched: {draggedItemName} -> {expectedPair}");
                            matchedCount++;
                            score += 20; // Add 20 points for each correct match
                        }
                        else
                        {
                            Debug.Log($"Mismatch: {draggedItemName} -> {expectedPair}");
                        }
                    }
                    else
                    {
                        Debug.Log($"No match found for dragged item: {draggedItemName}");
                    }
                }
                else
                {
                    // Handle empty slot
                    Debug.Log($"Slot {slot.name} is empty. Adding 0 score.");
                    score += 0; // Add 0 score for empty slot
                }
            }
            
            Debug.Log($"Matched pairs: {matchedCount}/{pairs.Count}");
            Debug.Log($"Final Score: {score}");
            ChallengeManager.Instance.CompleteChallenge(score);
            Destroy(this.gameObject);
        }
    }

    [System.Serializable]
    public class MatchData
    {
        public string challengeName;
        public List<Pair> pairs;
    }

    [System.Serializable]
    public class Pair
    {
        public string item;
        public string pair;
        public string itemImage; // New field for draggable item's image
        public string slotImage; // New field for slot's image
    }
}