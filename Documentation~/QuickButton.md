## QuickButtons
Draw buttons in the inspector without writing any editor code.

### Example
```csharp
public class QuickButtonsDemo : MonoBehaviour
{
    /// <summary>
    /// This draws a button in the inpsector that calls 
    /// OnDebugButtonClicked on click.
    /// </summary>
    public QuickButton NameButton = new QuickButton("OnDebugButtonClicked");
    
    /// <summary>
    /// This draws a button in the inpsector that invokes a  
    /// delegate on click.
    /// </summary>
    public QuickButton DelegateButton = new QuickButton(input =>
    {
        QuickButtonsDemo demo = input as QuickButtonsDemo;
        Debug.Log("Delegate Button Clicked on " + demo.gameObject.name);
    });
    
    private void OnDebugButtonClicked()
    {
        Debug.Log("Debug Button Clicked");
    }
}
```
![popup image](https://user-images.githubusercontent.com/20144789/54331762-d71dcc80-45f1-11e9-930a-38823c9ebc2e.png)
