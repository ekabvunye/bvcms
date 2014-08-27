CREATE NONCLUSTERED INDEX [IX_Attend_2] ON [dbo].[Attend] ([OrganizationId], [PeopleId], [MeetingDate] DESC)
GO
IF @@ERROR<>0 AND @@TRANCOUNT>0 ROLLBACK TRANSACTION
GO
IF @@TRANCOUNT=0 BEGIN INSERT INTO #tmpErrors (Error) SELECT 1 BEGIN TRANSACTION END
GO
