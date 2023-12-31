﻿using Discord.Commands;
using Discord.WebSocket;
using System.Text.RegularExpressions;

public class Commands : ModuleBase<SocketCommandContext>
{

    [Command("help")]
    public async Task HelpAsync() 
    {
        if (Context.User.Id != Program.settings.configuration.botAdminId)
        {
            return;
        }

        string latestVersionUrl = await Program.GetGithubText("https://raw.githubusercontent.com/Remi-Dv/informations/main/DownloadDiscordBot/DownloadUrl");
        string latestVersion = await Program.GetGithubText("https://raw.githubusercontent.com/Remi-Dv/informations/main/DownloadDiscordBot/LastVersion.txt");
        latestVersion = Regex.Replace(latestVersion, "[^0-9.]", "");

        await ReplyAsync($"Lastest version ({latestVersion}): ```{latestVersionUrl}```");
        await ReplyAsync("```   Debidy Help menu:\n" +
        "- !Help -> Help menu\n" +
        "- !ShareFile filePath -> Start sending a file\n" +
        "- !Downloads -> Open the downloads folder\n" +
        "- !Cancel -> Cancel the sending of a file```");
    }

    [Command("Give")]
    public async Task GiveAsync(string fileName)
    {
        if (Context.User.Id != Program.settings.configuration.otherBotId)
        {
            await ReplyAsync("Only the other bot can send files");
            return;
        }
        if (Context.Message.Attachments.Count == 1)
        {
            var attachment = Context.Message.Attachments.FirstOrDefault();
            if (attachment != null)
            {
                Program.filesStorage.CreateDirectory(Path.Combine(Program.filesStorage.filePartsReceivedPath, fileName));
                Program.downloader.AddCommand(fileName, attachment.Url, 0, Context);
            }
        }
        else
        {
            await ReplyAsync("There is not 1 attachement to this message!");
        }
    }
    

    [Command("ShareFile")]
    public async Task ShareFile(string filePath)
    {
        if (Context.User.Id != Program.settings.configuration.botAdminId)
        {
            return;
        }

        if (Program.fileSender != null)
        {
            await ReplyAsync("You are already sending a file!");
            return;
        }
        if (!File.Exists(filePath))
        {
            await ReplyAsync("This file doesn't exist!");
            return;
        }
        if (!Regex.IsMatch(Path.GetFileName(filePath), "^[a-zA-Z0-9.]+$"))
        {
            await ReplyAsync("error: The file contains others caracters than letters, LETTERS, numbers, dot");
            return;
        }
        SocketTextChannel channel = Program.discordBot.client.GetChannel(Context.Channel.Id) as SocketTextChannel;
        await ReplyAsync("Starting... (Do not use the file that you are sharing while sending)");
        Program.fileSender = new FileSender(filePath, channel);
    }

    [Command("Assemble")]
    public async Task AssembleAsync(string fileName, int chunkNumber)
    {
        if (Context.User.Id != Program.settings.configuration.otherBotId)
        {
            ReplyAsync("You are not the other bot!");
            return;
        }

        Program.downloader.AddCommand(fileName, null, chunkNumber, Context);
    }

    [Command("Ask")]
    public async Task Ask(string fileName, string missingFilesNumber)
    {
        if (Context.User.Id != Program.settings.configuration.otherBotId)
        {
            await ReplyAsync("You are not the other bot!");
            return;
        }
        if (Program.fileSender != null)
        {
            Program.fileSender.missingFiles = new List<string>();
            string[] missingFiles = missingFilesNumber.Split(',');
            for (int i = 0; i < missingFiles.Length; i++)
            {
                Program.fileSender.missingFiles.Add(missingFiles[i]);
            }
            Program.fileSender.tasks.Add(Task.Run(Program.fileSender.SendMissingFile));
        }
    }

    [Command("DeleteSendingCache")]
    public async Task DeleteSendingCacheAsync(string fileName)
    {
        if (Context.User.Id != Program.settings.configuration.otherBotId)
        {
            return;
        }
        string directoryPath = Path.Combine(Program.filesStorage.filePartsSendingPath, fileName);
        await ReplyAsync("Deleting Cache...");
        Program.filesStorage.DeleteDirectory(directoryPath);
        Program.fileSender.Cancel(Context);
        Program.fileSender = null;
    }

    [Command("Downloads")]
    public async Task OpenDownloadsFolderAsync()
    {
        if (Context.User.Id != Program.settings.configuration.botAdminId)
        {
            return;
        }
        await ReplyAsync("Trying to open...");
        if (!Program.filesStorage.OpenExplorer(Program.filesStorage.downloadsPath))
        {
            await ReplyAsync(@"Failed to open, you can find your downloads at 
                C:\Users\USER(modify to yours)\AppData\Roaming\RemiDv\Debidy\downloads");
        }
        else
        {
            await ReplyAsync("The folder is opened!");
        }
    }

    [Command("Cancel")]
    public async Task CancelAsync()
    {
        if (Context.User.Id != Program.settings.configuration.botAdminId)
        {
            return;
        }
        Program.fileSender.Cancel(Context);
        Program.fileSender = null;
    }

    [Command("DeleteReceivedCache")]
    public async Task DeleteReceivedCacheAsync(string fileName)
    {
        if (Context.User.Id != Program.settings.configuration.otherBotId)
        {
            return;
        }
        Program.downloader.AddCommand(fileName, null, 0, Context);

        //string directoryPath = Path.Combine(Program.filesStorage.filePartsReceivedPath, fileName);
        //Program.filesStorage.DeleteDirectory(directoryPath);
    }
}