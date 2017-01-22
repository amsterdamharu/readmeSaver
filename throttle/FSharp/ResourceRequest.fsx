module ResourceRequest

type ResourceRequest() =
    let replyChannel = ref []
    let message =
        MailboxProcessor.Start(fun inbox ->
            async { return () } )
    member this.Wait = message.Receive
    member this.Continue() = message.Post()
