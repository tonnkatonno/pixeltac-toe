module PixelTacToe.SkinUpload

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Html

[<Inline("new FileReader()")>]
let private newFileReader () : obj = X<obj>
[<Inline("new Image()")>]
let private newImage () : obj = X<obj>
[<Inline("$ctx.drawImage($img,0,0,$w,$h)")>]
let private drawImageThumb (ctx: obj) (img: obj) (w:int) (h:int) : unit = ()

[<JavaScript>]
module SkinUpload =

    let thumbSize   = 80
    let jpegQuality = 0.7
    let skinPacks   = Var.Create<List<List<string>>>([])

    let private shrinkImage (file: File) : Async<string> =
        async {
            let! dataUrl =
                Async.FromContinuations(fun (ok, err, _) ->
                    let r = newFileReader()
                    r?onload  <- fun _ -> ok  (string r?result)
                    r?onerror <- fun _ -> err (exn "read error")
                    r?readAsDataURL(file) |> ignore)

            let! thumb =
                Async.FromContinuations(fun (ok, err, _) ->
                    let img = newImage()
                    img?onload <- fun _ ->
                        let canvas = JS.Document.CreateElement("canvas") :?> HTMLCanvasElement
                        canvas.Width  <- thumbSize
                        canvas.Height <- thumbSize
                        let ctx = canvas.GetContext("2d")
                        drawImageThumb ctx img thumbSize thumbSize
                        ok (canvas.ToDataURL("image/jpeg", jpegQuality))
                    img?onerror <- fun _ -> err (exn "img error")
                    img?src <- dataUrl)

            return thumb
        }

    let onPackSelected (input: HTMLInputElement) =
        let maxFiles = 9
        if input.Files.Length > maxFiles then
            JS.Alert($"Only the first {maxFiles} images will be processed. The rest will be ignored.")
        let files =
            [ for i in 0 .. min (maxFiles-1) (input.Files.Length-1) -> input.Files.Item i ]
            |> List.choose (fun f -> if isNull f then None else Some f)

        async {
            let! thumbs = files |> List.map shrinkImage |> Async.Parallel
            skinPacks.Value <- skinPacks.Value @ [ thumbs |> Array.toList ]
        } |> Async.StartImmediate

    let uploadSkinPackView : Doc =
        input [
            attr.``type`` "file"
            attr.multiple "multiple"
            attr.accept   "image/*"
            on.change (fun el _ -> onPackSelected (el :?> HTMLInputElement))
        ] []