using UnityEngine;
using Nova;
using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class PauseUI : MonoBehaviour
{
    [Header("PANEL")]
    [SerializeField] private UIBlock2D pausePanel;
    
    [Header("SELECTION")]
    [InfoBox("Assign all selectionList", EInfoBoxType.Normal)]
    [SerializeField] private UIBlock2D[] selectionList;

    [Foldout("STYLING")][SerializeField] private int currentSelection = 0;
    [Foldout("STYLING")][SerializeField] private Color32 selectedColor = new Color32(255, 255, 100, 255);

    private Dictionary<UIBlock2D, Color32> originalColors = new Dictionary<UIBlock2D, Color32>();
    private Dictionary<UIBlock2D, TextBlock> textBlocks = new Dictionary<UIBlock2D, TextBlock>();
    private Dictionary<UIBlock2D, string> originalTexts = new Dictionary<UIBlock2D, string>();
    private Dictionary<UIBlock2D, Coroutine> colorRoutines = new Dictionary<UIBlock2D, Coroutine>();
    private Dictionary<UIBlock2D, Coroutine> scaleRoutines = new Dictionary<UIBlock2D, Coroutine>();
    
    [ReadOnly] public bool isPanelVisible = false;
    private bool isUsingKeyboard = true;
    
    void Start(){
        if(pausePanel != null) pausePanel.gameObject.SetActive(false);
        InitializeSelection();
    }
    
    void OnEnable(){
        if(GameManager.Instance != null){
            GameManager.Instance.isPaused = false;
            Time.timeScale = 1f;
        }
    }
    
    void Update(){
        if(GameManager.Instance == null) return;
        
        HandlePausePanel();
        
        if(GameManager.Instance.isPaused){
            HandleNavigation();
            HandleSelection();
            DetectInputMethod();
        }
    }
    
    void DetectInputMethod(){
        if(Keyboard.current != null){
            bool anyKeyPressed = Keyboard.current.upArrowKey.wasPressedThisFrame || 
                                 Keyboard.current.downArrowKey.wasPressedThisFrame ||
                                 Keyboard.current.wKey.wasPressedThisFrame ||
                                 Keyboard.current.sKey.wasPressedThisFrame ||
                                 Keyboard.current.enterKey.wasPressedThisFrame ||
                                 Keyboard.current.spaceKey.wasPressedThisFrame;
            
            if(anyKeyPressed) isUsingKeyboard = true;
        }
    }
    
    void HandlePausePanel(){
        if(pausePanel == null) return;
        
        if(GameManager.Instance.isPaused && !isPanelVisible){
            pausePanel.gameObject.SetActive(true);
            isPanelVisible = true;
            currentSelection = 0;
            isUsingKeyboard = true;
            UpdateAllSelections();
        }
        else if(!GameManager.Instance.isPaused && isPanelVisible){
            pausePanel.gameObject.SetActive(false);
            isPanelVisible = false;
        }
    }
    
    void InitializeSelection(){
        if(selectionList == null || selectionList.Length == 0) return;
        
        foreach(UIBlock2D block in selectionList){
            if(block == null) continue;
            
            originalColors[block] = block.Color;
            
            TextBlock textBlock = block.GetComponentInChildren<TextBlock>();
            if(textBlock != null){
                textBlocks[block] = textBlock;
                originalTexts[block] = textBlock.Text;
                textBlock.Text = originalTexts[block].ToLower();
            }
            
            block.AddGestureHandler<Gesture.OnPress>(OnButtonPressed);
            block.AddGestureHandler<Gesture.OnHover>(OnButtonHover);
            block.AddGestureHandler<Gesture.OnUnhover>(OnButtonUnhover);
        }
        
        UpdateAllSelections();
    }
    
    void HandleNavigation(){
        if(selectionList == null || selectionList.Length == 0) return;
        if(!isUsingKeyboard) return;
        
        float verticalInput = 0;
        if(Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame) verticalInput = 1;
        else if(Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame) verticalInput = -1;
        
        if(verticalInput != 0){
            int previousSelection = currentSelection;
            currentSelection = (currentSelection - (int)verticalInput + selectionList.Length) % selectionList.Length;
            
            if(currentSelection != previousSelection) UpdateAllSelections();
        }
    }
    
    void HandleSelection(){
        if(Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame){
            if(selectionList != null && currentSelection < selectionList.Length && selectionList[currentSelection] != null)
                ExecuteSelectedAction();
        }
    }
    
    void UpdateAllSelections(){
        for(int i = 0; i < selectionList.Length; i++){
            if(selectionList[i] == null) continue;
            
            bool isSelected = (i == currentSelection);
            UpdateButtonVisual(selectionList[i], isSelected);
        }
    }
    
    void UpdateButtonVisual(UIBlock2D button, bool isSelected){
        if(isSelected){
            SetColorImmediate(button, selectedColor);
            SetScaleImmediate(button, new Vector3(1.05f, 1.05f, 1f));
            if(textBlocks.ContainsKey(button)){
                TextBlock tb = textBlocks[button];
                if(tb != null && originalTexts.ContainsKey(button)){
                    string titleText = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(originalTexts[button].ToLower());
                    tb.Text = titleText;
                }
            }
        }
        else{
            SetColorImmediate(button, originalColors[button]);
            SetScaleImmediate(button, Vector3.one);
            if(textBlocks.ContainsKey(button)){
                TextBlock tb = textBlocks[button];
                if(tb != null && originalTexts.ContainsKey(button))
                    tb.Text = originalTexts[button].ToLower();
            }
        }
    }
    
    void SetColorImmediate(UIBlock2D target, Color32 targetColor){
        if(colorRoutines.ContainsKey(target) && colorRoutines[target] != null){
            StopCoroutine(colorRoutines[target]);
        }
        target.Color = targetColor;
        colorRoutines[target] = null;
    }
    
    void SetScaleImmediate(UIBlock2D target, Vector3 targetScale){
        if(scaleRoutines.ContainsKey(target) && scaleRoutines[target] != null){
            StopCoroutine(scaleRoutines[target]);
        }
        target.transform.localScale = targetScale;
        scaleRoutines[target] = null;
    }
    
    void ExecuteSelectedAction(){
        if(selectionList[currentSelection] == null) return;
        
        switch(currentSelection){
            case 0: Resume(); break;
            case 1: GoToSetting(); break;
            case 2: GoToMainMenu(); break;
            case 3: QuitGame(); break;
        }
    }

    void Resume(){
        GameManager.Instance.isPaused = false;
        Time.timeScale = 1f;
    }

    void GoToSetting(){
        Time.timeScale = 1f;
        GameManager.Instance.isPaused = false;
    }
    
    void GoToMainMenu(){
        Time.timeScale = 1f;
        GameManager.Instance.isPaused = false;
    }
    
    void QuitGame(){
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    void OnButtonPressed(Gesture.OnPress evt){
        UIBlock2D pressedButton = evt.Receiver as UIBlock2D;
        if(pressedButton == null) return;
        
        for(int i = 0; i < selectionList.Length; i++){
            if(selectionList[i] == pressedButton){
                currentSelection = i;
                isUsingKeyboard = false;
                UpdateAllSelections();
                ExecuteSelectedAction();
                break;
            }
        }
    }
    
    void OnButtonHover(Gesture.OnHover evt){
        UIBlock2D hoveredButton = evt.Receiver as UIBlock2D;
        if(hoveredButton == null) return;
        
        for(int i = 0; i < selectionList.Length; i++){
            if(selectionList[i] == hoveredButton){
                currentSelection = i;
                isUsingKeyboard = false;
                UpdateAllSelections();
                break;
            }
        }
    }
    
    void OnButtonUnhover(Gesture.OnUnhover evt){
        UIBlock2D unhoveredButton = evt.Receiver as UIBlock2D;
        if(unhoveredButton == null) return;
        
        if(!isUsingKeyboard){
            for(int i = 0; i < selectionList.Length; i++){
                if(selectionList[i] == unhoveredButton && i == currentSelection) return;
            }
        }
        
        if(!isUsingKeyboard && currentSelection < selectionList.Length) UpdateAllSelections();
    }
    
    void OnDestroy(){
        if(GameManager.Instance != null){
            GameManager.Instance.isPaused = false;
            Time.timeScale = 1f;
        }
    }
}