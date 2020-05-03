using System;
using UnityEngine;
using UnityEngine.UI;

public class Testbench : MonoBehaviour
{
    [SerializeField]
    private Logger logger;
    
    [SerializeField]
    private TMPro.TMP_InputField sourceInput;

    [SerializeField] private Button compileButton;

    [SerializeField]
    private VMGC vmgc;

    private VM vm;
    private Disassembler disassembler;
    private Chunk chunk;

    private bool runRepl = false;

    public void Start()
    {
        vm = new VM(vmgc, logger);
        compileButton.onClick.AddListener(OnCompileButtonClicked); 
    }

    public void OnCompileButtonClicked()
    {
        compileButton.interactable = false;
        sourceInput.interactable = false;
        RunSource(sourceInput.text);
    }

    private void RunFile(string filePath)
    {
        string source = System.IO.File.ReadAllText(@filePath);
        RunSource(source);
    }

    private void RunSource(string source)
    {
        logger.ClearLog();

        logger.LogPrint("Compiling source:\n\n{0}\n", source);
        source += "\n";

        vm = new VM(vmgc, logger);
        
        vm.OnRunFinished.AddListener(OnRunFinished);

        if (source != "" && source != null)
        {
            vm.Interpret(source);    
        }
        else
            logger.LogPrint("Could not read source.");
    }

    private void OnRunFinished(InterpretResult result)
    {
        logger.LogPrint("\nResult: {0}", result.ToString());
        compileButton.interactable = true;
        sourceInput.interactable = true;
    }
}
