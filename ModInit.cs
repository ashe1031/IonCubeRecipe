using BepInEx;
using Nautilus.Crafting;
using Nautilus.Handlers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using UnityEngine;

[BepInPlugin("com.ashe1031.ioncubemod", "Ion Cube Recipe", "1.0.0")]
public class IonCubeRecipeMod : BaseUnityPlugin
{
    private const string RecipeFileName = "IonCubeRecipe.json";

    private void Awake()
    {
        var pluginFolder = Path.Combine(Paths.PluginPath, "IonCubeRecipe");
        var recipePath = Path.Combine(pluginFolder, RecipeFileName);

        Directory.CreateDirectory(pluginFolder);

        // Load or create the recipe JSON
        RecipeJson recipeJson;
        if (File.Exists(recipePath))
        {
            try
            {
                var json = File.ReadAllText(recipePath);
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    var serializer = new DataContractJsonSerializer(typeof(RecipeJson));
                    recipeJson = (RecipeJson)serializer.ReadObject(ms);
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"Failed to load recipe JSON: {e}");
                recipeJson = RecipeJson.Default();
                WriteRecipeJson(recipePath, recipeJson);
            }
        }
        else
        {
            recipeJson = RecipeJson.Default();
            WriteRecipeJson(recipePath, recipeJson);
        }

        // Convert JSON to Nautilus RecipeData
        var recipeData = new RecipeData
        {
            craftAmount = recipeJson.CraftAmount,
            Ingredients = new List<Ingredient>()
        };
        foreach (var ing in recipeJson.Ingredients)
        {
            if (Enum.TryParse<TechType>(ing.TechType, out var techType))
            {
                recipeData.Ingredients.Add(new Ingredient(techType, ing.Amount));
            }
            else
            {
                Logger.LogWarning($"Unknown TechType: {ing.TechType} in IonCubeRecipe.json, skipping.");
            }
        }

        // Register the recipe in the Fabricator under Resources > Electronics
        CraftTreeHandler.AddCraftingNode(
            CraftTree.Type.Fabricator,
            TechType.PrecursorIonCrystal,
            "Resources", "Electronics"
        );
        CraftDataHandler.SetRecipeData(
            TechType.PrecursorIonCrystal,
            recipeData
        );

        // Make it unlockable when the player first picks up an Ion Cube
        KnownTechHandler.AddRequirementForUnlock(TechType.PrecursorIonCrystal, TechType.PrecursorIonCrystal);

        Logger.LogInfo("Ion Cube Recipe loaded!");
    }

    private void WriteRecipeJson(string path, RecipeJson recipeJson)
    {
        try
        {
            using (var fs = new FileStream(path, FileMode.Create))
            {
                var serializer = new DataContractJsonSerializer(typeof(RecipeJson));
                serializer.WriteObject(fs, recipeJson);
            }
            Logger.LogInfo($"Recipe JSON saved to {path}");
        }
        catch (Exception e)
        {
            Logger.LogError($"Failed to save recipe JSON: {e}");
        }
    }
}

[DataContract]
public class RecipeJson
{
    [DataMember]
    public int CraftAmount { get; set; }

    [DataMember]
    public List<IngredientJson> Ingredients { get; set; }

    public static RecipeJson Default()
    {
        return new RecipeJson
        {
            CraftAmount = 1,
            Ingredients = new List<IngredientJson>
            {
                new IngredientJson { TechType = "AdvancedWiringKit", Amount = 1 },
                new IngredientJson { TechType = "ComputerChip", Amount = 1 },
                new IngredientJson { TechType = "ReactorRod", Amount = 2 },
                new IngredientJson { TechType = "Kyanite", Amount = 3 }
            }
        };
    }
}

[DataContract]
public class IngredientJson
{
    [DataMember]
    public string TechType { get; set; }

    [DataMember]
    public int Amount { get; set; }
}