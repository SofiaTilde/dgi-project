using UnityEngine;
using Classes;

public class UI : MonoBehaviour
{
    public Button generate;
    public GameObject specimen;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Button.onClick.AddListener(OnClick);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnClick()
    {
        Program program = new Program(4, 5);
        specimen.GenerateSpecimen(program.internodes, program.petioles, program.leaves);
    }
}
