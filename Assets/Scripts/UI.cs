using UnityEngine;
using UnityEngine.UI;
using Classes;
using UnityEngine.UIElements;

public class UI : MonoBehaviour
{
    public UnityEngine.UI.Button generate;
    public UnityEngine.UI.Slider sliderAge;
    public UnityEngine.UI.Slider sliderLight;
    public GameObject specimen;
    public int ageValue;
    public int lightValue;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        generate.onClick.AddListener(OnClick);
    }

    // Update is called once per frame
    void Update()
    {
        // Set variables to values of age- and lightslider
        ageValue = (int)sliderAge.value;
        lightValue = (int)sliderLight.value;
    }

    void OnClick()
    {
        // Create instance of Program using the values 1-5 of age- and lightValue
        Program program = new Program(ageValue, lightValue);
        specimen.GetComponent<MeshFabricator>().GenerateSpecimenMesh(program.internodes, program.petioles, program.leaves);
    }
}
