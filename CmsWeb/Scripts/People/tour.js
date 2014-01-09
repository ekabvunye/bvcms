﻿$(function () {
    var $demo, duration, remaining, tour;
    tour = new Tour();
    duration = 5000;
    remaining = duration;
    tour.addSteps([
{
  title: "Welcome to the new Person Page!"
  , content: "This page is in beta, but we want you to use it. " +
      "This guided tour will point a few things out to you. " +
      "Click next to continue. " +
      "Once you have watched this tour, " +
      "you can <a id='tourdone' class='hide-tip' data-hidetip='person' href='#'>click here</a> so you won't see it again."
  , backdrop: true
  , orphan: true
}, {
  title: "Edit an Address"
  , element: "a.edit.editaddr:first"
  , placement: "right"
  , content: "Click the pencil icon. " +
      "The pencil is used throughout the new UI to indicate edit. " +
      "A dialog box will appear. You can add a personal address on that dialog."
}, {
  title: "We got Badges!"
  , element: "li.badges span:first"
  , placement: "bottom"
  , content: "You will see what we have been calling Status Flags as badges here. " +
      "The green ones are updated every night. The blue ones are for campus and membership"
}, {
  title: "The new blue toolbar"
  , element: 'div.btn-page-actions'
  , placement: "bottom"
  ,content: "This replaces the old green toolbar. " +
      "Email, reports and other actions are done from here."
}, {
  title: "Famliy Sidebar"
  ,element: "#family-div"
  , placement: "left"
  ,content: "This is where the family will show. " +
      "This replaces the separate family page. " +
      "The current family member will have a blue bar and a white background. " +
      "Click on another family member to bring up their page."
}, {
  title: "Add to Family"
  ,element: "#family-div a.searchadd"
  , placement: "left"
  ,content: "You will click the + to add a new family member."
}, {
  title: "Related Families"
  ,element: "#related-families-div"
  , placement: "left"
  ,content: "Related families show here. " +
      "Click the + to add a related family. " +
      "Click the pencil to edit the desription. " +
      "Click the family name to go to the head of that family."
}, {
  title: "Family Photo"
  ,element: "#family-photo-div"
  , placement: "left"
  ,content: "You can now have a family photo in addtion to the personal photo! " +
      "Click the + to upload a photo. " +
      "Click the picture to edit or delete an existing photo."
}, {
  title: "Want to know more?"
  ,content: "This ends this little show. " +
      "The next time it starts, you can tell the system that you don't need to see it anymore. " +
      "Be sure to <a href='http://www.youtube.com/bvcmscom' target='_blank'>" +
      "watch the videos we will be doing to introduce the new UI.</a>"
  ,backdrop: true
  ,orphan: true
}
    ]);
    tour.init();
    tour.restart();
    $("html").smoothScroll();
    $("#tourdone").live("click", function (ev) {
        ev.preventDefault();
        tour.end();
    });
});