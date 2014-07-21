﻿#if INTERACTIVE
#else
namespace global
#endif

type UnitType = 
    | Empty
    | Unit of string * int 
    | CompositeUnit of UnitType list     
    static member Create(s,n) =
        if n = 0 then Empty else Unit(s,n)
    override this.ToString() =
        let exponent = function
            | Empty -> 0
            | Unit(_,n) -> n
            | CompositeUnit(_) -> invalidOp ""
        let rec toString = function        
            | Empty -> ""
            | Unit(s,n) when n=0 -> ""
            | Unit(s,n) when n=1 -> s
            | Unit(s,n)          -> s + " ^ " + n.ToString()            
            | CompositeUnit(us) ->               
                let ps, ns =
                    us |> List.partition (fun u -> exponent u >= 0)
                let join xs = 
                    let s = xs |> List.map toString |> List.toArray             
                    System.String.Join(" ",s)
                match ps,ns with
                | ps, [] -> join ps
                | ps, ns ->
                    let ns = ns |> List.map UnitType.Reciprocal
                    join ps + " / " + join ns
        match this with
        | Unit(_,n) when n < 0 -> " / " + (this |> UnitType.Reciprocal |> toString)
        | _ -> toString this    
    static member ( * ) (v:ValueType,u:UnitType) = UnitValue(v,u)    
    static member ( * ) (lhs:UnitType,rhs:UnitType) =       
        let text = function
            | Empty -> ""                 
            | Unit(s,n) -> s
            | CompositeUnit(us) -> us.ToString()
        let normalize us u =
            let t = text u
            match us |> List.tryFind (fun x -> text x = t), u with
            | Some(Unit(s,n) as v), Unit(_,n') ->
                us |> List.map (fun x -> if x = v then UnitType.Create(s,n+n') else x)                 
            | Some(_), _ -> raise (new System.NotImplementedException())
            | None, _ -> us@[u]
        let normalize' us us' =
            us' |> List.fold (fun (acc) x -> normalize acc x) us
        match lhs,rhs with
        | Unit(u1,p1), Unit(u2,p2) when u1 = u2 ->
            UnitType.Create(u1,p1+p2)
        | Empty, _ -> rhs
        | _, Empty -> lhs 
        | Unit(u1,p1), Unit(u2,p2) ->            
            CompositeUnit([lhs;rhs])
        | CompositeUnit(us), Unit(_,_) ->
            CompositeUnit(normalize us rhs)
        | Unit(_,_), CompositeUnit(us) ->
            CompositeUnit(normalize' [lhs]  us)
        | CompositeUnit(us), CompositeUnit(us') ->
            CompositeUnit(normalize' us us')
        | _,_ -> raise (new System.NotImplementedException())
    static member Reciprocal x =
        let rec reciprocal = function
            | Empty -> Empty
            | Unit(s,n) -> Unit(s,-n)
            | CompositeUnit(us) -> CompositeUnit(us |> List.map reciprocal)
        reciprocal x
    static member ( / ) (lhs:UnitType,rhs:UnitType) =        
        lhs * (UnitType.Reciprocal rhs)
    static member ( + ) (lhs:UnitType,rhs:UnitType) =       
        if lhs = rhs then lhs                
        else invalidOp "Unit mismatch"   
and ValueType = decimal
and UnitValue (v:ValueType,u:UnitType) =
    new(v:ValueType) = UnitValue(v,Empty)
    new(v:ValueType,s:string) = UnitValue(v,Unit(s,1))
    member this.Value = v
    member this.Unit = u
    override this.ToString() = sprintf "%O %O" v u
    static member (~-) (v:UnitValue) =
        UnitValue(-v.Value,v.Unit)
    static member (+) (lhs:UnitValue,rhs:UnitValue) =
        UnitValue(lhs.Value+rhs.Value, lhs.Unit+rhs.Unit)         
    static member (-) (lhs:UnitValue,rhs:UnitValue) =
        UnitValue(lhs.Value-rhs.Value, lhs.Unit+rhs.Unit) 
    static member (*) (lhs:UnitValue,rhs:UnitValue) =                    
        UnitValue(lhs.Value*rhs.Value,lhs.Unit*rhs.Unit)                
    static member (*) (lhs:UnitValue,rhs:ValueType) =        
        UnitValue(lhs.Value*rhs,lhs.Unit)      
    static member (*) (v:UnitValue,u:UnitType) = 
        UnitValue(v.Value,v.Unit*u)  
    static member (/) (lhs:UnitValue,rhs:UnitValue) =                    
        UnitValue(lhs.Value/rhs.Value,lhs.Unit/rhs.Unit)
    static member (/) (lhs:UnitValue,rhs:ValueType) =
        UnitValue(lhs.Value/rhs,lhs.Unit)  
    static member (/) (v:UnitValue,u:UnitType) =
        UnitValue(v.Value,v.Unit/u)
    static member Pow (lhs:UnitValue,rhs:UnitValue) =
        let isInt x = 0.0M = x - (x |> int |> decimal)
        let areAllInts =
            List.forall (function (Unit(_,p)) -> isInt (decimal p*rhs.Value) | _ -> false)      
        let toInts =            
            List.map (function (Unit(s,p)) -> Unit(s, int (decimal p * rhs.Value)) | _ -> invalidOp "" )
        match lhs.Unit, rhs.Unit with
        | Empty, Empty -> 
            let x = (float lhs.Value) ** (float rhs.Value)           
            UnitValue(decimal x)
        | _, Empty when isInt rhs.Value ->
            pown lhs (int rhs.Value)
        | Unit(s,p1), Empty when isInt (decimal p1*rhs.Value) ->
            let x = (float lhs.Value) ** (float rhs.Value)
            UnitValue(x |> decimal, Unit(s,int (decimal p1*rhs.Value)))       
        | CompositeUnit us, Empty when areAllInts us -> 
            let x = (float lhs.Value) ** (float rhs.Value)
            UnitValue(x |> decimal, CompositeUnit(toInts us))
        | _ -> invalidOp "Unit mismatch"
    static member One = UnitValue(1.0M,Empty) 
    override this.Equals(that) =
        let that = that :?> UnitValue
        this.Unit = that.Unit && this.Value = that.Value
    override this.GetHashCode() = hash this 
    interface System.IComparable with
        member this.CompareTo(that) =
            let that = that :?> UnitValue
            if this.Unit = that.Unit then
                if this.Value < that.Value then -1
                elif this.Value > that.Value then 1
                else 0
            else invalidOp "Unit mismatch"

[<AutoOpen>]
module Tokenizer =

    type token =
        | WhiteSpace
        | Symbol of char
        | OpToken of string
        | StrToken of string
        | NumToken of string

    let (|Match|_|) pattern input =
        let m = System.Text.RegularExpressions.Regex.Match(input, pattern)
        if m.Success then Some m.Value else None

    let matchToken = function
        | Match @"^\s+" s -> s, WhiteSpace
        | Match @"^\+|^\-|^\*|^\/|^\^"  s -> s, OpToken s
        | Match @"^=|^<>|^<=|^>=|^>|^<"  s -> s, OpToken s   
        | Match @"^\(|^\)|^\,|^\:" s -> s, Symbol s.[0]
        | Match @"^[A-Za-z]+" s -> s, StrToken s
        | Match @"^\d+(\.\d+)?|\.\d+" s -> s, s |> NumToken
        | _ -> invalidOp "Failed to match token"

    let tokenize s =
        let rec tokenize' index (s:string) =
            if index = s.Length then [] 
            else
                let next = s.Substring index 
                let text, token = matchToken next
                token :: tokenize' (index + text.Length) s
        tokenize' 0 s
        |> List.choose (function WhiteSpace -> None | t -> Some t)

[<AutoOpen>]
module Parser =

    type arithmeticOp = Add | Sub | Mul | Div
    type formula =
        | Neg of formula
        | Exp of formula * formula
        | ArithmeticOp of formula * arithmeticOp * formula
        | Num of UnitValue

    let rec (|Term|_|) = function
        | Exponent(f1, t) ->      
            let rec aux f1 = function        
                | SumOp op::Exponent(f2, t) -> aux (ArithmeticOp(f1,op,f2)) t               
                | t -> Some(f1, t)      
            aux f1 t  
        | _ -> None
    and (|SumOp|_|) = function 
        | OpToken "+" -> Some Add | OpToken "-" -> Some Sub 
        | _ -> None
    and (|Exponent|_|) = function
        | Factor(b, OpToken "^"::Exponent(e,t)) -> Some(Exp(b,e),t)
        | Factor(f,t) -> Some (f,t)
        | _ -> None
    and (|Factor|_|) = function  
        | OpToken "-"::Factor(f, t) -> Some(Neg f, t)
        | Atom(f1, ProductOp op::Factor(f2, t)) ->
            Some(ArithmeticOp(f1,op,f2), t)       
        | Atom(f, t) -> Some(f, t)  
        | _ -> None    
    and (|ProductOp|_|) = function
        | OpToken "*" -> Some Mul | OpToken "/" -> Some Div
        | _ -> None
    and (|Atom|_|) = function    
        | Symbol '('::Term(f, Symbol ')'::t) -> Some(f, t)
        | Number(n,t) -> Some(n,t)
        | Units(u,t) -> Some(Num u,t)  
        | _ -> None
    and (|Number|_|) = function
        | NumToken n::Units(u,t) -> Some(Num(u * decimal n),t)
        | NumToken n::t -> Some(Num(UnitValue(decimal n)), t)      
        | _ -> None
    and (|Units|_|) = function
        | Unit'(u,t) ->
            let rec aux u1 =  function
                | OpToken "/"::Unit'(u2,t) -> aux (u1 / u2) t
                | Unit'(u2,t) -> aux (u1 * u2) t
                | t -> Some(u1,t)
            aux u t
        | _ -> None
    and (|Int|_|) s = 
        match System.Int32.TryParse(s) with
        | true, n -> Some n
        | false,_ -> None
    and (|Unit'|_|) = function  
        | StrToken u::OpToken "^"::OpToken "-"::NumToken(Int p)::t -> 
            Some(UnitValue(1.0M,UnitType.Create(u,-p)),t)  
        | StrToken u::OpToken "^"::NumToken(Int p)::t -> 
            Some(UnitValue(1.0M,UnitType.Create(u,p)),t)
        | StrToken u::t ->
            Some(UnitValue(1.0M,u), t)
        | _ -> None

    let parse s = 
        match tokenize s with
        | Term(f,[]) -> f 
        | _ -> failwith "Failed to parse formula"

    let evaluate formula =
        let rec eval = function
            | Neg f -> - (eval f)
            | Exp(b,e) -> (eval b) ** (eval e)
            | ArithmeticOp(f1,op,f2) -> arithmetic op (eval f1) (eval f2)        
            | Num d -> d
        and arithmetic = function
            | Add -> (+) | Sub -> (-) | Mul -> (*) | Div -> (/)      
        eval formula
            