// Sort list based on input and ascending
function sortList() {
    const sort = document.getElementById("sortInput").value;
    const ascending = document.getElementById("ascendingInput").checked;
    const list = document.getElementsByTagName("table")[0];
    let sorting = true;
    let shouldSort = false;

    while (sorting) {
        sorting = false;
        let content = list.getElementsByTagName("tbody")[0].getElementsByTagName("tr");

        for (i = 0; i < (content.length - 1); i++) {
            let item = content[i].querySelector(`#${sort}`).innerHTML.toLowerCase();
            let nextItem = content[i + 1].querySelector(`#${sort}`).innerHTML.toLowerCase();
            shouldSort = false;

            if ((item > nextItem && ascending == true) || (item < nextItem && ascending == false)) {
                shouldSort = true;
                break;
            }
        }
        if (shouldSort) {
            content[i].parentNode.insertBefore(content[i + 1], content[i]);
            sorting = true;
        }
    }
}