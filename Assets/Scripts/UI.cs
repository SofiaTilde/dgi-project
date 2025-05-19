using UnityEngine;
using UnityEngine.UI;
using Classes;
using UnityEngine.UIElements;

public class UI : MonoBehaviour
{
    public UnityEngine.UI.Button generate;
    public GameObject specimen;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        generate.onClick.AddListener(OnClick);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnClick()
    {
        Program program = new Program(5, 5);
        specimen.GetComponent<MeshFabricator>().GenerateSpecimenMesh(program.internodes, program.petioles, program.leaves);
    }
}
