using UnityEngine;
using UnityEngine.UI;
using Classes;
using UnityEngine.UIElements;
using TMPro;

public class UI : MonoBehaviour
{
    public UnityEngine.UI.Button generate;
    public UnityEngine.UI.Button reGenerate;
    public UnityEngine.UI.Slider sliderAge;
    public UnityEngine.UI.Slider sliderLight;
    public GameObject specimen;
    public new GameObject light;
    public int ageValue;
    public int lightValue;

    [SerializeField] private TextMeshProUGUI ageSliderText = null;
    [SerializeField] private TextMeshProUGUI lightSliderText = null;

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
        ageSliderText.text = ageValue.ToString("0");
        lightValue = (int)sliderLight.value;
        lightSliderText.text = lightValue.ToString("0");
        light.GetComponent<Light>().intensity = lightValue * 0.2f + 0.5f;
    }

    void OnClick()
    {
        // Create instance of Program using the values 1-5 of age- and lightValue
        Program program = new Program(ageValue, lightValue);
        specimen.GetComponent<MeshFabricator>().GenerateSpecimenMesh(program.internodes, program.petioles, program.leaves);
    }
}
