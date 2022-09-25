using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using UnityEngine;
using static Constants;

public class MonsterParts : MonoBehaviour
{
    public static string[] partsDir = Directory.GetDirectories(PARTS_PATH);
    public static int numOfPartsCategories = partsDir.Length;
    public static string[] partsCategoryNames = new string[numOfPartsCategories];
    public static List<GameObject[]> partsList = new List<GameObject[]>();
    public static List<string> partsFolderNames = new List<string>(Directory.GetDirectories(PARTS_PREFABS_PATH));
    public static int HEAD_INDEX = 1;   // This is the category part that is the head (MAY CHANGE BASED ON FOLDER ORDER)

    public Transform[] transforms;

    public static void InitializeMonsterParts()
    {
        // Initial parts data
        for (int i = 0; i < numOfPartsCategories; i++)
        {
            // Get category labels
            Regex regex = new Regex(PARTS_PATH + '/');
            partsCategoryNames[i] = regex.Replace(partsDir[i], "");

            // Get category folder names
            regex = new Regex(Path.GetDirectoryName(partsFolderNames[i]) + '/');
            partsFolderNames[i] = PARTS_PREFABS_PATH_SHORT + regex.Replace(partsFolderNames[i], "");

            // Add prefabs to each category
            partsList.Add(InitializePartsPrefabs(partsFolderNames[i]));
        }
    }

    // Fill parts prefab arrays
    public static GameObject[] InitializePartsPrefabs(string dirName)
    {
        return Resources.LoadAll<GameObject>(dirName);
    }

    // Set body part of a monster
    public void SetBodyPart(int categoryIndex, int partIndex, Monster monster)
    {
        Transform tf = transforms[categoryIndex];

        if (monster.bodyParts[categoryIndex] != null)
            Destroy(monster.bodyParts[categoryIndex]);

        if (partIndex >= partsList[categoryIndex].Length)
        {
            monster.bodyPartsInt[categoryIndex] = partIndex;
            return;
        }

        monster.bodyParts[categoryIndex] = Instantiate(partsList[categoryIndex][partIndex], tf);
        monster.bodyPartsInt[categoryIndex] = partIndex;
    }

    // Randomize body parts
    public static int[] RandomizeParts()
    {
        int[] partsInt = new int[numOfPartsCategories];

        for (int i = 0; i < numOfPartsCategories; i++)
            partsInt[i] = Random.Range(0, partsList[i].Length);

        return partsInt;
    }

    // Randomize base color
    public static Color RandomizeBaseColor()
    {
        float randomRed = Random.Range(0, 1.0f);
        float randomGreen = Random.Range(0, 1.0f);
        float randomBlue = Random.Range(0, 1.0f);

        return new Color(randomRed, randomGreen, randomBlue, 1);
    }

    // Create monster head
    public static void CreatePortrait(Monster monster, Transform mainTransform, Transform baseTransform, bool moveHead = false)
    {
        for (int i = 0; i < monster.bodyParts.Length; i++)
        {
            // Do not instantiate a part if player chose none for it
            if (monster.bodyPartsInt[i] >= partsList[i].Length)
                continue;

            monster.bodyParts[i] = Instantiate(partsList[i][monster.bodyPartsInt[i]], mainTransform);
        }

        // Deal with head
        if (monster.bodyPartsInt[HEAD_INDEX] < partsList[HEAD_INDEX].Length)
        {
            if (moveHead)
                monster.bodyParts[HEAD_INDEX].transform.parent = baseTransform;
            else
                monster.bodyParts[HEAD_INDEX].GetComponent<SpriteRenderer>().color = monster.baseColor;
        }

        // Color base
        foreach (SpriteRenderer sprite in baseTransform.GetComponentsInChildren<SpriteRenderer>())
            sprite.color = monster.baseColor;
    }
}
