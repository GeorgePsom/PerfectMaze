using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{

    public InputField widthValue, heightValue, cellXValue, cellZValue, Enemies;
    public Button generateButton, playButton;
    public GridGraph grid;
    public MazeGenerator generator;
    public Toggle useGPU;
    public Dropdown miniResolutionDropdown;
    private Action Generate;
    public GameLogic gameLogic;
    
    void Start()
    {
        Generate =  useGPU.isOn ? new Action(generator.ParallelGenerate) : new Action(generator.Generate);
        generator.useGPU = useGPU.isOn ? true : false;
        gameLogic.useGPU = generator.useGPU;
        int resolution = GetMiniResoltionFromDropdown();
        generator.solver.miniXCell = resolution - 1;
        generator.solver.miniZCell = resolution - 1;
        widthValue.text = grid.Width.ToString();
        heightValue.text = grid.Height.ToString();
        cellXValue.text = grid.xCells.ToString();
        cellZValue.text = grid.zCells.ToString();
        Enemies.text = gameLogic.nEnemies.ToString();
    }

    int GetMiniResoltionFromDropdown()
    {
        string text = miniResolutionDropdown.options[miniResolutionDropdown.value].text;
        switch(text)
        {
            case "2x2":
                return 2;
            case "4x4":
                return 4;
            case "8x8":
                return 8;
            case "16x16":
                return 16;
            case "32x32":
                return 32;
            case "64x64":
                return 64;
        }
        return 2; // Default
       
        
    }

    public void onSelectingMiniResolution()
    {
        int resolution = GetMiniResoltionFromDropdown();
        generator.solver.miniXCell = resolution - 1;
        generator.solver.miniZCell = resolution - 1;
    }
    public void SetWidth(string width)
    {
        widthValue.text = width;
        grid.Width = Int32.Parse(width);
    }

    public void SetHeight(string height)
    {
        heightValue.text = height;
        grid.Height = Int32.Parse(height);
    }

    public void SetCellX(string cellX)
    {
        cellXValue.text = cellX;
        grid.xCells = Int32.Parse(cellX);
    }


    public void SetCellZ(string cellZ)
    {
        cellZValue.text = cellZ;
        grid.zCells = Int32.Parse(cellZ);
    }

    public void onCheckingGPU()
    {
        Generate = useGPU.isOn ? new Action(generator.ParallelGenerate) : new Action(generator.Generate);
        
    }

    public void SetEnemies(string n)
    {
        Enemies.text = n;
        gameLogic.nEnemies = Int32.Parse(Enemies.text);
    }

    public void onPlay()
    {
        if(!useGPU.isOn)
    
            gameLogic.ActivatePlayMode();
    }

    public void OnGenerate()
    {
        generator.Destroy();
        generator.useGPU = useGPU.isOn;
        gameLogic.useGPU = useGPU.isOn;
        Generate();
       
    }
}
