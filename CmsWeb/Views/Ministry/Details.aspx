<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage<CmsData.Ministry>" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <h2>Detail</h2>

    <fieldset>
        <legend>Fields</legend>
        <p>
            MinistryId:
            <%= Html.Encode(Model.MinistryId) %>
        </p>
        <p>
            MinistryName:
            <%= Html.Encode(Model.MinistryName) %>
        </p>
        <p>
            MinistryDescription:
            <%= Html.Encode(Model.MinistryDescription) %>
        </p>
    </fieldset>
    <p>

        <%=Html.ActionLink("Edit", "Edit", new { id=Model.MinistryId }) %> |
        <%=Html.ActionLink("Delete", "Delete", new { id=Model.MinistryId }) %> |
        <%=Html.ActionLink("Back to List", "Index") %>
    </p>

</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="head" runat="server">
</asp:Content>

