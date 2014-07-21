namespace UnitsCalculator

open System
open System.Drawing

open MonoTouch.Foundation
open MonoTouch.UIKit

[<Register ("UnitsCalculatorViewController")>]
type UnitsCalculatorViewController () =
    inherit UIViewController ()

    override x.DidReceiveMemoryWarning () =
        // Releases the view if it doesn't have a superview.
        base.DidReceiveMemoryWarning ()
        // Release any cached data, images, etc that aren't in use.

    override x.ViewDidLoad () =
        base.ViewDidLoad ()
        x.View.BackgroundColor <- UIColor.Gray;

        let h = 31.0f;
        let w = x.View.Bounds.Width;

        let formulaField = 
            new UITextField(        
                Placeholder = "Enter formula",
                BorderStyle = UITextBorderStyle.RoundedRect,
                Frame = new RectangleF(10.0f, 30.0f, w - 20.0f, h))

        x.View.AddSubview(formulaField)

        let resultField =
            new UITextField(        
                Placeholder = "Result",
                BorderStyle = UITextBorderStyle.RoundedRect,
                Frame = new RectangleF(10.0f, 130.0f, w - 20.0f, h))
        
        x.View.AddSubview(resultField)

        let submitButton = UIButton.FromType(UIButtonType.RoundedRect)
        submitButton.Frame <- new RectangleF(10.0f, 80.0f, w - 20.0f, 44.0f)
        submitButton.BackgroundColor <- UIColor.Green
        submitButton.SetTitle("Submit", UIControlState.Normal)
        submitButton.TouchUpInside.AddHandler(fun _ _ ->
            let f = parse formulaField.Text
            let v = evaluate f
            resultField.Text <- v.ToString()
        )

        x.View.AddSubview(submitButton)

    override x.ShouldAutorotateToInterfaceOrientation (toInterfaceOrientation) =
        // Return true for supported orientations
        if UIDevice.CurrentDevice.UserInterfaceIdiom = UIUserInterfaceIdiom.Phone then
           toInterfaceOrientation <> UIInterfaceOrientation.PortraitUpsideDown
        else
           true

