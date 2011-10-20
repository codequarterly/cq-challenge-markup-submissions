module StringEx

open System
open System.Text


type String with
    /// Return true if the strings  is null or empty, false otherwise
    static member isEmpty = String.IsNullOrWhiteSpace

    /// Trim whitespace from the end of the string s
    static member trimEnd (s:string) = s.TrimEnd()

    /// Replace all occurrencews of oldValue with newValue in the string s
    static member replace (oldValue:string) (newValue:string) (s:string) = s.Replace(oldValue, newValue)

    /// Return a new string consisting of n spaces
    static member ofSpaces n = new String(' ',n)

    /// Return the string s indented by n spaces
    static member indent n (s:String) = new String(' ',n) + s

    /// Return the index of the first character in the string s that satisfies the predicate f
    static member indexOfFirst f s =
        let rec iter i =
            if (i = String.length s) || (f s.[i]) then i
            else iter (i+1)
        iter 0

    /// Return the first char in a string
    static member firstCharIn (s:string) = s.[0]

    /// Return true, if the string s contains the substring sub
    static member contains (sub:string) (s:string) = s.Contains(sub)

    /// Crop a string to a maximum width, adding ellipsis if the string was shortened
    static member crop maxWidth s =
        let len = String.length s
        if (len <= maxWidth) then s
        else if maxWidth <= 3 then "..."
             else s.Substring(0, min maxWidth len) + "..."


type StringBuilder with
    /// Return the contents as a string with any whitespace removed from the end
    member this.TrimEnd() = this.ToString().TrimEnd()

    /// Returns the contents as a string and clears the contents
    member this.Consume() =
        let s = this.ToString()
        this.Clear() |> ignore
        s

    /// Appends a character to the contents
    member this.AddChar (c:char) = this.Append(c) |> ignore
