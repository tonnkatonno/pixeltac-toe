namespace PixelTacToe

open System
open WebSharper
open WebSharper.UI
open WebSharper.UI.Client
open WebSharper.UI.Html
open PixelTacToe.SkinUpload.SkinUpload

[<AutoOpen>]

[<JavaScript>]
module Client =
     
    type Player = X | O
    let player1 = Var.Create ""
    let player2 = Var.Create ""
    let starter = Var.Create X  

    let lastWinner = Var.Create None

    let rand = System.Random()
    let rollDice () = 1 + rand.Next(6)

    let namesSet = Var.Create false
    let board : Var<Option<Player>[,]> = Var.Create(Array2D.create 3 3 None)
    let currentPlayer = Var.Create X
    let diceRollAnim = Var.Create<Option<int>>(None)
    let showDiceRoll = Var.Create false
    let chosenSkin1 = Var.Create<Option<string>>(None)
    let chosenSkin2 = Var.Create<Option<string>>(None)
    
    let wins1, wins2 = Var.Create 0, Var.Create 0 

    [<Inline("localStorage.getItem($key)")>]
    let lsGet (key:string) : string = Unchecked.defaultof<string>

    [<Inline("localStorage.setItem($key,$value)")>]
    let lsSet (key:string) (value:string) : unit = Unchecked.defaultof<unit>
    
    let resetGame () =
        match lastWinner.Value with
        | Some X ->
            starter.Value <- O
            currentPlayer.Value <- O
            board.Value <- Array2D.create 3 3 None
            showDiceRoll.Value <- false
            diceRollAnim.Value <- None
        | Some O ->
            starter.Value <- X
            currentPlayer.Value <- X
            board.Value <- Array2D.create 3 3 None
            showDiceRoll.Value <- false
            diceRollAnim.Value <- None
        | None ->
            showDiceRoll.Value <- true
            board.Value <- Array2D.create 3 3 None
            diceRollAnim.Value <- None

        
    let handleDiceRoll () =
        let result = rollDice ()
        diceRollAnim.Value <- Some result
        async {
            do! Async.Sleep 1500
            if result % 2 = 0 then
                starter.Value <- X
                currentPlayer.Value <- X
            else
                starter.Value <- O
                currentPlayer.Value <- O

            board.Value <- Array2D.create 3 3 None
            diceRollAnim.Value <- None
            showDiceRoll.Value <- false
        } |> Async.Start

    let playerToStr = function X -> "X" | O -> "O"
    
    let playerToImg = function
        | X -> chosenSkin1.Value 
        | O -> chosenSkin2.Value
            
    let selectMode () =
        if List.isEmpty skinPacks.Value then false 
        else chosenSkin1.Value.IsNone || chosenSkin2.Value.IsNone

    let renderCell (i:int) (j:int) (b:Option<Player>[,]) =
        let idx = i * 3 + j
        if selectMode () then
            match List.tryItem idx (skinPacks.Value |> List.concat) with
            | Some url ->
                let alreadyChosen =
                    chosenSkin1.Value = Some url || chosenSkin2.Value = Some url
                img [
                    attr.src url
                    attr.style
                        (sprintf "width:60px;height:60px;object-fit:cover;\
                                   border:3px solid %s;cursor:%s;"
                            (match chosenSkin1.Value, chosenSkin2.Value with
                             | Some u, _ when u = url -> "#4caf50" 
                             | _, Some u when u = url -> "#e53935"  
                             | _ -> "#ccc")
                            (if alreadyChosen then "not-allowed" else "pointer"))
                    on.click (fun _ _ ->
                        if not alreadyChosen then
                            match chosenSkin1.Value, chosenSkin2.Value with
                            | None, _       -> chosenSkin1.Value <- Some url
                            | Some _, None  -> chosenSkin2.Value <- Some url
                            | _             -> ())
                ] []
            | None -> text ""
        else
            match b.[i,j] with
            | Some p ->
                match playerToImg p with
                | Some url ->
                    img [ attr.src url
                          attr.style "width:60px;height:60px;object-fit:contain;" ] []
                | None ->
                    text (playerToStr p)
            | None -> text ""

    let diceRollView : Doc =
        div [attr.id "dice-view"] [
            h2 [] [text "We will now choose the starter by tossing."]
            div [attr.style "font-size: 45px; font-family: monospace; margin: 20px;"]
                [ textView (diceRollAnim.View.Map (function Some n -> sprintf "🎲 %d" n | None -> "🎲 ?")) ]
            button 
                [
                    on.click (fun _ _ -> handleDiceRoll ())
                ] 
                [
                    text "Throwing"
                ]
        ]

    let skinPackChooser
        (chosen : Var<Option<string>>)
        (packIdx : int, pack : List<string>) =

        div [ attr.style "margin:4px 0;" ] [
            text (sprintf "Pack #%d:" (packIdx + 1))

            div [ attr.style "display:grid;\
                               grid-template-columns:repeat(3,70px);\
                               gap:4px;margin-top:4px;" ] [
            yield! pack |> List.map (fun src ->
                img [
                    attr.src src
                    attr.styleDyn (
                        chosen.View
                        |> View.Map (fun sel ->
                            let border =
                                match sel with
                                | Some s when s = src ->
                                    if obj.ReferenceEquals(chosen, chosenSkin1)
                                    then "#4caf50" else "#e53935"
                                | _ -> "#ccc"
                            sprintf "width:70px;height:70px;object-fit:cover; border:2px solid %s;cursor:pointer;" border))
                    on.click (fun _ _ -> chosen.Value <- Some src)
                ] [])
            ]
        ]


    let nameInputView : Doc =
        div [] [
            h2 [] [ text "Players & skin packs" ]
            uploadSkinPackView
            skinPacks.View
            |> View.Map (fun packs ->
                div [] [
                    div [ attr.style "margin-top:10px;" ] [
                        yield label [] [ text "X player name: " ]
                        let nameBoxStyle (skinChosen: Var<Option<string>>) =
                            attr.styleDyn (
                                skinChosen.View
                                |> View.Map (function
                                    | Some _ -> "border:2px solid " + (if obj.ReferenceEquals(skinChosen, chosenSkin1) then "#4caf50;" else "#e53935;")
                                    | None   -> "border:2px solid #ccc;"))
                        yield Doc.InputType.Text
                                [ nameBoxStyle chosenSkin1
                                  attr.placeholder "First player name" ] player1
                        yield! (packs
                                |> List.mapi (fun i p ->
                                    skinPackChooser chosenSkin1 (i, p)))
                    ]

                    div [ attr.style "margin-top:14px;" ] [
                        yield label [] [ text "O player name: " ]
                        let nameBoxStyle (skinChosen: Var<Option<string>>) =
                            attr.styleDyn (
                                skinChosen.View
                                |> View.Map (function
                                    | Some _ -> "border:2px solid " + (if obj.ReferenceEquals(skinChosen, chosenSkin2) then "#4caf50;" else "#e53935;")
                                    | None   -> "border:2px solid #ccc;"))
                        yield Doc.InputType.Text
                                [ nameBoxStyle chosenSkin1
                                  attr.placeholder "Second player name" ] player2
                        yield! (packs
                                |> List.mapi (fun i p ->
                                    skinPackChooser chosenSkin2 (i, p)))
                    ]
                ])
            |> Doc.EmbedView

            let namesOk =
                View.Map2
                    (fun n1 n2 ->
                        not (String.IsNullOrWhiteSpace n1) &&
                        not (String.IsNullOrWhiteSpace n2))
                    player1.View player2.View
            let chosenOk =
                View.Map2 (fun (c1: Option<string>) (c2: Option<string>) ->
                               Option.isSome c1 && Option.isSome c2)
                           chosenSkin1.View chosenSkin2.View
            let packsOk =
                View.Const true
            let canStart =
                View.Map3 (fun a b c -> a && b && c)
                          namesOk chosenOk packsOk
            button [
                attr.style "margin-top:12px;"
                on.click (fun _ _ ->
                    namesSet.Value <- true
                    showDiceRoll.Value <- true)
            ] [ text "Start" ]
        ]
        
    let checkWinner (b : Option<Player>[,]) =
        let checkLine (l : Option<Player> list) =
            match l with
            | [Some X; Some X; Some X] -> Some X
            | [Some O; Some O; Some O] -> Some O
            | _ -> None
        let rows  = [ for i in 0 .. 2 -> [ b.[i,0]; b.[i,1]; b.[i,2] ] ]
        let cols  = [ for j in 0 .. 2 -> [ b.[0,j]; b.[1,j]; b.[2,j] ] ]
        let diags = [ [ b.[0,0]; b.[1,1]; b.[2,2] ]; [ b.[0,2]; b.[1,1]; b.[2,0] ] ]
        (rows @ cols @ diags) |> List.tryPick checkLine
    
    let renderBoard () =
        board.View
        |> View.Map (fun b ->
            div [ attr.``class`` "game-board" ] [
                for i in 0 .. 2 do
                    for j in 0 .. 2 ->
                        button [
                            attr.``class`` "cell"
                            if Option.isSome b.[i,j] then attr.disabled "disabled"
                            on.click (fun _ _ ->
                                if not (selectMode ()) &&
                                   Option.isNone b.[i,j] &&
                                   checkWinner b |> Option.isNone then
                                    let nb = Array2D.copy b
                                    nb.[i,j] <- Some currentPlayer.Value
                                    board.Value <- nb
                                    if checkWinner nb |> Option.isNone then
                                        currentPlayer.Value <-
                                            (match currentPlayer.Value with X -> O | O -> X))
                        ] [ renderCell i j b ]
            ])
        |> Doc.BindView id


    let isFull (b : Option<Player>[,]) =
        seq {
            for i in 0 .. 2 do
                for j in 0 .. 2 do
                    yield b.[i,j]
        } |> Seq.forall Option.isSome

    let scoreboard =
        div [ attr.style "margin-left:20px;" ] [
            h3 [] [ text "Results" ]
            p [] [
                textView (
                    View.Map2 (fun name w -> sprintf "%s: %d" name w)
                              player1.View wins1.View)
            ]
            p [] [
                textView (
                    View.Map2 (fun name w -> sprintf "%s: %d" name w)
                              player2.View wins2.View)
            ]
        ]
    
    [<SPAEntryPoint>]
    let Main () =

        skinPacks.Value <-
            match lsGet "packs" with
            | null | "" -> []
            | json      -> Json.Deserialize<List<List<string>>> json

        chosenSkin1.Value <-
            match lsGet "c1" with null | "" -> None | url -> Some url

        chosenSkin2.Value <-
            match lsGet "c2" with null | "" -> None | url -> Some url

        skinPacks.View   |> View.Sink (fun p -> lsSet "packs" (Json.Serialize p))
        chosenSkin1.View |> View.Sink (fun v -> lsSet "c1" (defaultArg v ""))
        chosenSkin2.View |> View.Sink (fun v -> lsSet "c2" (defaultArg v ""))

        View.Sink (fun b ->
            match checkWinner b with
            | Some X ->
                wins1.Value <- wins1.Value + 1
                lastWinner.Value <- Some X
                showDiceRoll.Value <- false
            | Some O ->
                wins2.Value <- wins2.Value + 1
                lastWinner.Value <- Some O
                showDiceRoll.Value <- false
            | None when isFull b ->
                async {
                    do! Async.Sleep 1500
                    lastWinner.Value <- None
                    showDiceRoll.Value <- true
                } |> Async.Start
            | _ -> ()
        ) board.View


        let statusText : View<string> =
            View.Map3 (fun b w1 w2 ->
                match checkWinner b with
                | Some p ->
                    let n = if p = X then player1.Value else player2.Value
                    sprintf "Wins: %s (%s). Current game status—%s: %d | %s: %d"
                            n (playerToStr p) player1.Value w1 player2.Value w2
                | None when isFull b ->
                    "A roll of the dice follows"
                | None ->
                    let n = if currentPlayer.Value = X then player1.Value else player2.Value
                    sprintf "Next player moves: %s (%s)" n (playerToStr currentPlayer.Value)
            ) board.View wins1.View wins2.View

        View.Map2 (fun namesSet diceRoll ->
            if not namesSet then
                nameInputView
            elif diceRoll then
                diceRollView
            else
                div [ attr.id "game-container" ] [
                    h1 [] [ text "TicTacToe" ]
                    renderBoard ()
                    div [ attr.id "score-wrapper" ] [ scoreboard ]
                    div [ attr.``class`` "status" ] [ textView statusText ]
                    button [ on.click (fun _ _ -> resetGame()) ] [ text "New Game" ]
                ]
        ) namesSet.View showDiceRoll.View
        |> Doc.EmbedView
        |> Doc.RunById "main"