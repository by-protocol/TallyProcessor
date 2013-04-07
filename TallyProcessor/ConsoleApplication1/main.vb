Module Module1

    Const defaultConf As String = "conf\config.xml"

    Sub Main(ByVal args() As String)
        Dim conf As configprocessor = Nothing
        ' erstmal einen std. logger einrichten bis die aus der config geladen werden
        logger.addLogAction(logger.stdLogAction)

        Try
            If args.Length > 0 Then
                For Each arg In args
                    If My.Computer.FileSystem.FileExists(arg) Then
                        logger.log("Reading config: " & arg)
                        conf = New configprocessor(arg)
                        conf.open()
                        Exit For
                    Else
                        logger.err("No config found at: " & arg)
                        Console.ReadKey()
                    End If
                Next
            End If

            If conf Is Nothing Then
                If My.Computer.FileSystem.FileExists(defaultConf) Then
                    logger.warn("No config given, try standard.")
                    conf = New configprocessor(defaultConf)
                    conf.open()
                Else
                    logger.err("Could not find config. Nothing left to do.")
                    logger.close()
                    Console.WriteLine("Press any key to exit")
                    Console.ReadKey()
                    System.Environment.Exit(2)
                End If
            End If


            While Console.ReadKey().KeyChar <> "q"
                Console.WriteLine("Press q to exit")
            End While

            conf.close()
            logger.close()
            System.Environment.Exit(0)

        Catch e As Exception
            logger.critical(e.ToString)
            conf.close()
            logger.close()
            Console.WriteLine("Press any key to exit")
            Console.ReadKey()
            System.Environment.Exit(1)
        End Try
    End Sub

End Module
