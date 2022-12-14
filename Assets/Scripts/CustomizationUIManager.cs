using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static Traits;
using static GameConfig;
using static SaveManager;
using static Constants;

public class CustomizationUIManager : MonoBehaviour
{
    [Header("INFO")]
    public TMP_InputField nameField;
    public TMP_InputField ageField;

    [Header("TRAITS")]
    public GameObject traitToggleObj;
    public Transform traitsTransform;
    public float traitsToggleSpacing;

    [Header("PARTS")]
    public MonsterParts monsterParts;
    public GameObject partsDropdownObj;
    public Transform appearanceTransform;
    public Transform baseTransform;
    public float partsDropdownSpacing;

    [Header("COLORS")]
    public Color defaultTextColor;
    public Color darkTextColor;
    public Color highlightedTextColor;
    public Color disabledTextColor;

    [Header("Color Sliders")]
    public Slider sliderRed;
    public Slider sliderGreen;
    public Slider sliderBlue;

    [Header("UI Objects")]
    public TMP_Text portraitHeader;
    public TMP_Text portraitTraits;
    public Button finishButton;

    private List<TMP_Dropdown> partsDropdowns;
    private Toggle[] traitToggles;
    private bool fullTraits = false;
    private Color disabledButtonTextColor;
    private SpriteRenderer[] baseParts;

    private string PORTRAIT_HEADER_TEXT;
    private string TRAITS_UI_TEXT;
    private string FILE_TYPE = ".png";

    private void Awake()
    {
        MonsterParts.InitializeMonsterParts();
        InitializePlayer();
    }

    private void Start()
    {
        // Set text templates
        TRAITS_UI_TEXT = traitsTransform.GetComponentInChildren<TMP_Text>().text;
        PORTRAIT_HEADER_TEXT = portraitHeader.text;

        // Set initial info
        nameField.text = player.name;
        ageField.text = player.age;

        disabledButtonTextColor = finishButton.GetComponentInChildren<TMP_Text>().color;

        UpdatePortraitTraits();
        PopulateTraitsToggles();
        PopulatePartsDropdown();
        InitializeParts();
        SetInitialColorSlider(player.baseColor);
        SetBaseColor();
    }

    // Make sure player is valid before allowing user to continue
    public bool PlayerIsValid()
    {
        return player.name != "" && player.age != "" && fullTraits; 
    }

    public void UpdateTraitsCountUI()
    {
        traitsTransform.GetComponentInChildren<TMP_Text>().text = string.Format(TRAITS_UI_TEXT, player.traits.Count, numOfTraits);
    }

    // Update name on portrait as player types in name
    public void UpdateInfoText()
    {
        portraitHeader.text = string.Format(PORTRAIT_HEADER_TEXT, nameField.text, ageField.text);
    }

    // Save player info
    public void SaveInfo()
    {
        player.name = nameField.text;
        player.age = ageField.text;
        SetButtonState();
    }

    // Populate Traits field with toggles
    public void PopulateTraitsToggles()
    {
        UpdateTraitsCountUI();

        Transform column1 = traitsTransform.Find(COLUMN1_NAME);
        Transform column2 = traitsTransform.Find(COLUMN2_NAME);

        traitToggles = new Toggle[traits.Count];

        // Create toggles for each 
        for (int i = 0; i < traits.Count / 2; i++)
        {
            // Instantiate toggles on first column
            GameObject obj = Instantiate(traitToggleObj,
                new Vector2(column1.position.x, column1.position.y + traitsToggleSpacing * i),
                Quaternion.identity,
                column1);

            // Instatiate toggles on second column
            GameObject obj2 = Instantiate(traitToggleObj,
                new Vector2(column2.position.x, column2.position.y + traitsToggleSpacing * i),
                Quaternion.identity,
                column2);

            // Set trait names for toggles
            obj.GetComponentInChildren<Text>().text = traits[i];
            obj2.GetComponentInChildren<Text>().text = traits[i + traits.Count / 2];

            // Store Toggles in array
            traitToggles[i] = obj.GetComponent<Toggle>();
            traitToggles[i + traits.Count / 2] = obj2.GetComponent<Toggle>();
        }

        // Create listener to listen to changes
        foreach (Toggle tog in traitToggles)
        {
            tog.onValueChanged.AddListener(delegate
            {
                OnClickTraitToggle(tog);
            });
        }
    }

    // Manage toggles upon clicking
    public void OnClickTraitToggle(Toggle toggle)
    {
        int toggleIndex = System.Array.IndexOf(traitToggles, toggle);
        int opposingTraitIndex = GetOppositeTraitIndex(toggleIndex);

        if (player.traits.Count <= numOfTraits)
        {
            // Determine what happens when clicked
            if (toggle.isOn)
            {
                // Add traits to respective lists
                player.traits.Add(toggleIndex);
                player.opposingTraits.Add(opposingTraitIndex);

                // Disable opposing trait's toggle
                traitToggles[opposingTraitIndex].interactable = false;

                // Set appropriate colors
                traitToggles[toggleIndex].GetComponentInChildren<Text>().color = highlightedTextColor;
                traitToggles[opposingTraitIndex].GetComponentInChildren<Text>().color = disabledTextColor;
            }
            else
            {
                // Remove traits from respective lists
                player.traits.Remove(toggleIndex);
                player.opposingTraits.Remove(opposingTraitIndex);

                // Reenable opposing trait's toggle
                traitToggles[opposingTraitIndex].interactable = true;

                // Set appropriate colors
                traitToggles[toggleIndex].GetComponentInChildren<Text>().color = defaultTextColor;
                traitToggles[opposingTraitIndex].GetComponentInChildren<Text>().color = defaultTextColor;

                // Reenable disabled toggles from when traits was last full
                if (fullTraits)
                {
                    for (int i = 0; i < traitToggles.Length; i++)
                    {
                        if (!player.traits.Contains(i) && !player.opposingTraits.Contains(i))
                        {
                            traitToggles[i].interactable = true;
                            traitToggles[i].GetComponentInChildren<Text>().color = defaultTextColor;
                        }
                    }
                }
            }
        }

        fullTraits = player.traits.Count >= numOfTraits;
        UpdateTraitsCountUI();

        // Disable all other traits if limit is reached
        if (fullTraits)
        {
            for (int i = 0; i < traitToggles.Length; i++)
            {
                if (!player.traits.Contains(i))
                {
                    traitToggles[i].interactable = false;
                    traitToggles[i].GetComponentInChildren<Text>().color = disabledTextColor;
                }
            }
        }

        UpdatePortraitTraits();
        SetButtonState();
    }

    private void UpdatePortraitTraits()
    {
        string portraitText = "";

        foreach (int i in player.traits)
            portraitText += traits[i] + "\n";

        portraitTraits.text = portraitText;
    }

    private void SetButtonState()
    {
        if (PlayerIsValid())
        {
            finishButton.GetComponentInChildren<TMP_Text>().color = defaultTextColor;
            finishButton.interactable = true;
        }
        else
        {
            finishButton.GetComponentInChildren<TMP_Text>().color = disabledButtonTextColor;
            finishButton.interactable = false;
        }
    }

    // Populate dropdown with body parts
    public void PopulatePartsDropdown()
    {
        partsDropdowns = new List<TMP_Dropdown>();

        // Create new dropdown objects depending on number of traits indicated
        for (int i = 0; i < SaveManager.monsterParts.numOfPartsCategories; i++)
        {
            // Instantiate dropdown object
            GameObject obj = Instantiate(partsDropdownObj,
                new Vector2(appearanceTransform.position.x, appearanceTransform.position.y + partsDropdownSpacing * i),
                Quaternion.identity,
                appearanceTransform);

            // Set category label
            obj.transform.GetComponentInChildren<TMP_Text>().text = SaveManager.monsterParts.partsCategoryNames[i];

            // Populate dropdown with parts
            partsDropdowns.Add(obj.GetComponent<TMP_Dropdown>());
            List<string> partsNames = new List<string>();

            // Get monster parts names
            for (int j = 0; j < monsterParts.partsList[i].Length; j++)
                partsNames.Add(monsterParts.partsList[i][j].name);

            partsNames.Add(NONE_TEXT);  // Add 'None' option for no part
            partsDropdowns[i].AddOptions(partsNames);   // Add parts names to list
            partsDropdowns[i].value = player.bodyPartsInt[i]; // Set initial values of dropdown
            monsterParts.SetBodyPart(i, partsDropdowns[i].value, player);   // Instantiate body parts on card
        }
    }

    /*
    // Populate dropdown with body parts
    public void PopulatePartsDropdown()
    {
        partsDropdowns = new List<TMP_Dropdown>();

        // Create new dropdown objects depending on number of traits indicated
        for (int i = 0; i < SaveManager.monsterParts.numOfPartsCategories; i++)
        {
            // Instantiate dropdown object
            GameObject obj = Instantiate(partsDropdownObj,
                new Vector2(appearanceTransform.position.x, appearanceTransform.position.y + partsDropdownSpacing * i),
                Quaternion.identity,
                appearanceTransform);

            // Set category label
            obj.transform.GetComponentInChildren<TMP_Text>().text = SaveManager.monsterParts.partsCategoryNames[i];

            // Populate dropdown with parts
            partsDropdowns.Add(obj.GetComponent<TMP_Dropdown>());
            List<string> partsPath = new List<string>(Directory.GetFiles(SaveManager.monsterParts.partsDir[i], "*" + FILE_TYPE));
            List<string> partsNames = new List<string>();

            // Format part names for dropdown
            foreach (string str in partsPath)
            {
                Regex regex = new Regex(SaveManager.monsterParts.partsDir[i] + '/');
                partsNames.Add(regex.Replace(str, "").Replace(FILE_TYPE, ""));
            }

            partsNames.Add(NONE_TEXT);  // Add 'None' option for no part
            partsDropdowns[i].AddOptions(partsNames);   // Add parts names to list
            partsDropdowns[i].value = player.bodyPartsInt[i]; // Set initial values of dropdown
            monsterParts.SetBodyPart(i, partsDropdowns[i].value, player);   // Instantiate body parts on card
        }
    }*/

    // Initialize each parts category
    private void InitializeParts()
    {
        foreach (TMP_Dropdown dd in partsDropdowns)
        {
            dd.onValueChanged.AddListener(delegate
            {
                OnPartsDropdownChanged(dd);
            });
        }
    }

    public void OnPartsDropdownChanged(TMP_Dropdown dd)
    {
        int categoryIndex = partsDropdowns.IndexOf(dd);
        monsterParts.SetBodyPart(categoryIndex, partsDropdowns[categoryIndex].value, player);
        SetBaseColor();
    }

    private void SetInitialColorSlider(Color color)
    {
        sliderRed.value = color.r;
        sliderGreen.value = color.g;
        sliderBlue.value = color.b;
    }

    public void SetBaseColor()
    {
        baseParts = baseTransform.GetComponentsInChildren<SpriteRenderer>();
        player.baseColor = new Color(sliderRed.value, sliderGreen.value, sliderBlue.value, 1);

        foreach (SpriteRenderer sprite in baseParts)
            sprite.color = player.baseColor;
    }

    public void FinalizePlayer()
    {
        if (PlayerIsValid())
            GameController.MoveToScene(APP_SCENE);
    }
}
