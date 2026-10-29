(function () {
    "use strict";

    // Computes the insertion index for a drop based on the Y coordinate of the
    // pointer and the midpoints of the cards currently rendered in the column.
    // The card being dragged is excluded because the server also removes it
    // before inserting it at the requested position.
    function getDropIndex(container, clientY) {
        if (!container) {
            return 0;
        }

        var cards = Array.prototype.slice.call(
            container.querySelectorAll(".kanban-card:not(.is-dragging)"));
        var index = 0;

        for (var i = 0; i < cards.length; i++) {
            var rect = cards[i].getBoundingClientRect();
            if (clientY > rect.top + rect.height / 2) {
                index++;
            }
        }

        return index;
    }

    window.kanbelaDnd = { getDropIndex: getDropIndex };

    // Firefox only starts a native drag when dragstart writes something into
    // the dataTransfer object. Bootstrap the payload from the card id.
    document.addEventListener("dragstart", function (event) {
        var card = event.target.closest(".kanban-card");
        if (!card || !event.dataTransfer) {
            return;
        }

        event.dataTransfer.effectAllowed = "move";
        event.dataTransfer.setData("text/plain", card.dataset.cardId || "");
    });

    // Prevent the browser from navigating when a card is dropped outside a
    // valid drop zone.
    document.addEventListener("dragover", function (event) {
        if (event.target.closest(".column-cards")) {
            event.preventDefault();
        }
    });

    document.addEventListener("drop", function (event) {
        event.preventDefault();
    });
})();
